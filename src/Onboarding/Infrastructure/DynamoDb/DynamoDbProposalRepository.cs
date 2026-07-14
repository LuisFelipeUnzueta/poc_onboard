using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Globalization;
using Microsoft.Extensions.Options;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Proposals;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.DynamoDb;

public sealed class DynamoDbProposalRepository(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbOptions> options,
    ICorrelationContext correlationContext) : IProposalRepository
{
    private const string MetadataSk = "#METADATA";
    private const string ActiveCnpjSk = "#ACTIVE";
    private readonly string _tableName = options.Value.TableName;

    public async Task<bool> ExistsActiveByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken)
    {
        var response = await dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            IndexName = "CnpjIndex",
            KeyConditionExpression = "GSI2PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new($"CNPJ#{cnpj.Value}")
            },
            Limit = 1
        }, cancellationToken);

        return response.Count > 0;
    }

    public async Task AddAsync(Proposal proposal, CancellationToken cancellationToken)
    {
        try
        {
            var transactItems = new List<TransactWriteItem>
            {
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = ToMetadataItem(proposal),
                        ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                    }
                },
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new($"CNPJ#{proposal.Cnpj.Value}"),
                            ["SK"] = new(ActiveCnpjSk),
                            ["Type"] = new("CnpjUnique"),
                            ["ProposalId"] = new(proposal.Id.Value)
                        },
                        ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                    }
                }
            };
            transactItems.AddRange(CreateOutboxWrites(proposal));

            await dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            }, cancellationToken);

            proposal.ClearDomainEvents();
        }
        catch (TransactionCanceledException exception) when (exception.CancellationReasons.Any(reason => reason.Code == "ConditionalCheckFailed"))
        {
            throw new ProposalAlreadyExistsException();
        }
    }

    public async Task<ProposalDetailsResponse?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken)
    {
        var proposal = await GetAggregateByIdAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return null;
        }

        return new ProposalDetailsResponse(
            proposal.Id.Value,
            proposal.Status.ToString(),
            proposal.Cnpj.Value,
            proposal.LegalName.Value,
            proposal.Segment.ToString(),
            proposal.Documents.Select(document => new ProposalDocumentResponse(
                document.DocumentType,
                "Received",
                document.UploadedAt)).ToArray(),
            proposal.CreatedAt,
            proposal.UpdatedAt);
    }

    public async Task<Proposal?> GetAggregateByIdAsync(ProposalId proposalId, CancellationToken cancellationToken)
    {
        var response = await dynamoDb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new($"PROPOSAL#{proposalId.Value}")
            }
        }, cancellationToken);

        if (response.Count == 0)
        {
            return null;
        }

        var metadata = response.Items.SingleOrDefault(item => item["SK"].S == MetadataSk);
        if (metadata is null)
        {
            return null;
        }

        var documents = response.Items
            .Where(item => item["SK"].S.StartsWith("DOC#", StringComparison.Ordinal))
            .Select(ToDocument)
            .ToArray();

        return ToProposal(metadata, documents);
    }

    public async Task SaveDocumentUploadAsync(Proposal proposal, ProposalDocument document, CancellationToken cancellationToken)
    {
        try
        {
            var transactItems = new List<TransactWriteItem>
            {
                new()
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = ToDocumentItem(proposal.Id, document),
                        ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                    }
                },
                new()
                {
                    Update = new Update
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new($"PROPOSAL#{proposal.Id.Value}"),
                            ["SK"] = new(MetadataSk)
                        },
                        UpdateExpression = "SET #status = :status, GSI1PK = :gsi1pk, GSI1SK = :gsi1sk, UpdatedAt = :updatedAt, Version = :version",
                        ConditionExpression = "attribute_exists(PK) AND attribute_exists(SK)",
                        ExpressionAttributeNames = new Dictionary<string, string> { ["#status"] = "Status" },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":status"] = new(proposal.Status.ToString()),
                            [":gsi1pk"] = new($"STATUS#{proposal.Status}"),
                            [":gsi1sk"] = new($"{proposal.CreatedAt:O}#{proposal.Id.Value}"),
                            [":updatedAt"] = new(proposal.UpdatedAt.ToString("O")),
                            [":version"] = new() { N = proposal.Version.ToString(CultureInfo.InvariantCulture) }
                        }
                    }
                }
            };
            transactItems.AddRange(CreateOutboxWrites(proposal));

            await dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            }, cancellationToken);

            proposal.ClearDomainEvents();
        }
        catch (TransactionCanceledException exception) when (exception.CancellationReasons.Any(reason => reason.Code == "ConditionalCheckFailed"))
        {
            throw new DocumentAlreadyUploadedException();
        }
    }

    private static Proposal ToProposal(Dictionary<string, AttributeValue> item, IReadOnlyList<ProposalDocument> documents)
    {
        var partners = item["Partners"].L.Select(ToPartner).ToArray();
        var bankAccount = ToBankAccount(item["BankAccount"].M);
        var address = ToAddress(item["Address"].M);

        return Proposal.Rehydrate(
            ProposalId.Create(item["ProposalId"].S).Value!,
            PartnerId.Create(item["PartnerId"].S).Value!,
            Cnpj.Create(item["Cnpj"].S).Value!,
            LegalName.Create(item["LegalName"].S).Value!,
            Enum.Parse<Segment>(item["Segment"].S),
            Mcc.Create(item["Mcc"].S).Value!,
            Enum.Parse<ProposalStatus>(item["Status"].S),
            partners,
            bankAccount,
            address,
            documents,
            DateTimeOffset.Parse(item["CreatedAt"].S),
            DateTimeOffset.Parse(item["UpdatedAt"].S),
            int.Parse(item["Version"].N, CultureInfo.InvariantCulture));
    }

    private static ProposalDocument ToDocument(Dictionary<string, AttributeValue> item)
    {
        return new ProposalDocument(
            DocumentId.Create(item["DocumentId"].S).Value!,
            Enum.Parse<DocumentType>(item["DocumentType"].S),
            S3Key.Create(item["S3Key"].S).Value!,
            DateTimeOffset.Parse(item["UploadedAt"].S));
    }

    private static Partner ToPartner(AttributeValue item)
    {
        var partner = item.M;

        return Partner.Create(
            partner["Name"].S,
            Cpf.Create(partner["Cpf"].S).Value!,
            decimal.Parse(partner["ParticipationPercentage"].N, CultureInfo.InvariantCulture),
            partner["IsLegalRepresentative"].BOOL).Value!;
    }

    private static BankAccount ToBankAccount(Dictionary<string, AttributeValue> item)
    {
        return BankAccount.Create(
            item["Ispb"].S,
            item["Agency"].S,
            item["AccountNumber"].S,
            item["AccountDigit"].S,
            Enum.Parse<BankAccountType>(item["AccountType"].S)).Value!;
    }

    private static Address ToAddress(Dictionary<string, AttributeValue> item)
    {
        var complement = item["Complement"].NULL ? null : item["Complement"].S;

        return Address.Create(
            item["ZipCode"].S,
            item["Street"].S,
            item["Number"].S,
            complement,
            item["Neighborhood"].S,
            item["City"].S,
            item["State"].S).Value!;
    }

    private static Dictionary<string, AttributeValue> ToDocumentItem(ProposalId proposalId, ProposalDocument document) =>
        new()
        {
            ["PK"] = new($"PROPOSAL#{proposalId.Value}"),
            ["SK"] = new($"DOC#{document.DocumentType}"),
            ["Type"] = new("ProposalDocument"),
            ["DocumentId"] = new(document.Id.Value),
            ["ProposalId"] = new(proposalId.Value),
            ["DocumentType"] = new(document.DocumentType.ToString()),
            ["S3Key"] = new(document.S3Key.Value),
            ["Status"] = new("Received"),
            ["UploadedAt"] = new(document.UploadedAt.ToString("O"))
        };

    private IEnumerable<TransactWriteItem> CreateOutboxWrites(Proposal proposal) =>
        OutboxMessageFactory.Create(proposal.DomainEvents, proposal.Version, correlationContext.CorrelationId)
            .Select(message => new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = _tableName,
                    Item = ToOutboxItem(message),
                    ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                }
            });

    private static Dictionary<string, AttributeValue> ToOutboxItem(OutboxMessage message) => new()
    {
        ["PK"] = new($"OUTBOX#{message.EventId}"),
        ["SK"] = new(MetadataSk),
        ["GSI3PK"] = new("OUTBOX#PENDING"),
        ["GSI3SK"] = new($"{message.OccurredAt:O}#{message.EventId}"),
        ["Type"] = new("Outbox"),
        ["Status"] = new("PENDING"),
        ["EventId"] = new(message.EventId),
        ["EventType"] = new(message.EventType),
        ["AggregateId"] = new(message.AggregateId),
        ["AggregateType"] = new(message.AggregateType),
        ["OccurredAt"] = new(message.OccurredAt.ToString("O")),
        ["CorrelationId"] = new(message.CorrelationId),
        ["Version"] = new() { N = message.Version.ToString(CultureInfo.InvariantCulture) },
        ["Topic"] = new(message.Topic),
        ["Payload"] = new(message.Payload)
    };

    private static Dictionary<string, AttributeValue> ToMetadataItem(Proposal proposal) =>
        new()
        {
            ["PK"] = new($"PROPOSAL#{proposal.Id.Value}"),
            ["SK"] = new(MetadataSk),
            ["GSI1PK"] = new($"STATUS#{proposal.Status}"),
            ["GSI1SK"] = new($"{proposal.CreatedAt:O}#{proposal.Id.Value}"),
            ["GSI2PK"] = new($"CNPJ#{proposal.Cnpj.Value}"),
            ["GSI2SK"] = new($"PROPOSAL#{proposal.Id.Value}"),
            ["Type"] = new("Proposal"),
            ["ProposalId"] = new(proposal.Id.Value),
            ["PartnerId"] = new(proposal.PartnerId.Value),
            ["Cnpj"] = new(proposal.Cnpj.Value),
            ["LegalName"] = new(proposal.LegalName.Value),
            ["Segment"] = new(proposal.Segment.ToString()),
            ["Mcc"] = new(proposal.Mcc.Value),
            ["Status"] = new(proposal.Status.ToString()),
            ["CreatedAt"] = new(proposal.CreatedAt.ToString("O")),
            ["UpdatedAt"] = new(proposal.UpdatedAt.ToString("O")),
            ["Version"] = new() { N = proposal.Version.ToString(CultureInfo.InvariantCulture) },
            ["Partners"] = new() { L = proposal.Partners.Select(ToPartnerAttribute).ToList() },
            ["BankAccount"] = ToBankAccountAttribute(proposal),
            ["Address"] = ToAddressAttribute(proposal)
        };

    private static AttributeValue ToPartnerAttribute(Partner partner) =>
        new()
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["Name"] = new(partner.Name),
                ["Cpf"] = new(partner.Cpf.Value),
                ["ParticipationPercentage"] = new() { N = partner.ParticipationPercentage.ToString(CultureInfo.InvariantCulture) },
                ["IsLegalRepresentative"] = new() { BOOL = partner.IsLegalRepresentative }
            }
        };

    private static AttributeValue ToBankAccountAttribute(Proposal proposal) =>
        new()
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["Ispb"] = new(proposal.BankAccount.Ispb),
                ["Agency"] = new(proposal.BankAccount.Agency),
                ["AccountNumber"] = new(proposal.BankAccount.AccountNumber),
                ["AccountDigit"] = new(proposal.BankAccount.AccountDigit),
                ["AccountType"] = new(proposal.BankAccount.AccountType.ToString())
            }
        };

    private static AttributeValue ToAddressAttribute(Proposal proposal) =>
        new()
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["ZipCode"] = new(proposal.Address.ZipCode),
                ["Street"] = new(proposal.Address.Street),
                ["Number"] = new(proposal.Address.Number),
                ["Complement"] = string.IsNullOrWhiteSpace(proposal.Address.Complement) ? new AttributeValue { NULL = true } : new AttributeValue(proposal.Address.Complement),
                ["Neighborhood"] = new(proposal.Address.Neighborhood),
                ["City"] = new(proposal.Address.City),
                ["State"] = new(proposal.Address.State)
            }
        };
}
