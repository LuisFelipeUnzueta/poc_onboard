using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Proposals;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.DynamoDb;

public sealed class DynamoDbProposalRepository(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbOptions> options) : IProposalRepository
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
            await dynamoDb.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems =
                [
                    new TransactWriteItem
                    {
                        Put = new Put
                        {
                            TableName = _tableName,
                            Item = ToMetadataItem(proposal),
                            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                        }
                    },
                    new TransactWriteItem
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
                ]
            }, cancellationToken);
        }
        catch (TransactionCanceledException exception) when (exception.CancellationReasons.Any(reason => reason.Code == "ConditionalCheckFailed"))
        {
            throw new ProposalAlreadyExistsException();
        }
    }

    public async Task<ProposalDetailsResponse?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken)
    {
        var response = await dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new($"PROPOSAL#{proposalId.Value}"),
                ["SK"] = new(MetadataSk)
            }
        }, cancellationToken);

        if (response.Item.Count == 0)
        {
            return null;
        }

        var item = response.Item;
        return new ProposalDetailsResponse(
            item["ProposalId"].S,
            item["Status"].S,
            item["Cnpj"].S,
            item["LegalName"].S,
            item["Segment"].S,
            [],
            DateTimeOffset.Parse(item["CreatedAt"].S),
            DateTimeOffset.Parse(item["UpdatedAt"].S));
    }

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
            ["Version"] = new() { N = proposal.Version.ToString() },
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
                ["ParticipationPercentage"] = new() { N = partner.ParticipationPercentage.ToString(System.Globalization.CultureInfo.InvariantCulture) },
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
