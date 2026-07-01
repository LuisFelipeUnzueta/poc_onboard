using Microsoft.Extensions.Configuration;
using Onboarding.Application.Abstractions;
using Onboarding.Domain.Enums;

namespace Onboarding.Application.Proposals;

public sealed class DocumentRulesService(IConfiguration configuration) : IDocumentRulesService
{
    public IReadOnlyList<DocumentType> GetRequiredDocuments(Segment segment)
    {
        var rules = configuration.GetSection("DocumentRules").Get<IReadOnlyList<DocumentRuleOptions>>() ?? [];
        var rule = rules.FirstOrDefault(item => string.Equals(item.Segment, segment.ToString(), StringComparison.OrdinalIgnoreCase));

        return rule?.RequiredDocuments
            .Select(document => Enum.Parse<DocumentType>(document, ignoreCase: true))
            .ToArray() ?? [];
    }

    private sealed class DocumentRuleOptions
    {
        public string Segment { get; init; } = string.Empty;
        public IReadOnlyList<string> RequiredDocuments { get; init; } = [];
    }
}
