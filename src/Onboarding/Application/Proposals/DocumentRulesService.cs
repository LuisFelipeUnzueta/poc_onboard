using Microsoft.Extensions.Configuration;
using Onboarding.Application.Abstractions;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Enums;

namespace Onboarding.Application.Proposals;

public sealed class DocumentRulesService(IConfiguration configuration) : IDocumentRulesService
{
    public IReadOnlyList<DocumentType> GetRequiredDocuments(Segment segment, PersonType personType)
    {
        var rules = configuration.GetSection("DocumentRules").Get<IReadOnlyList<DocumentRuleOptions>>() ?? [];
        var rule = rules.FirstOrDefault(item =>
            string.Equals(item.Segment, segment.ToString(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.PersonType, personType.ToString(), StringComparison.OrdinalIgnoreCase));

        return rule?.RequiredDocuments
            .Select(document => Enum.Parse<DocumentType>(document, ignoreCase: true))
            .ToArray() ?? [];
    }

    public bool AreAllRequiredDocumentsUploaded(Proposal proposal)
    {
        var requiredDocuments = GetRequiredDocuments(proposal.Segment, PersonType.LegalPerson);
        var uploadedDocuments = proposal.Documents.Select(document => document.DocumentType).ToHashSet();

        return requiredDocuments.Count > 0 && requiredDocuments.All(uploadedDocuments.Contains);
    }

    private sealed class DocumentRuleOptions
    {
        public string Segment { get; init; } = string.Empty;
        public string PersonType { get; init; } = string.Empty;
        public IReadOnlyList<string> RequiredDocuments { get; init; } = [];
    }
}
