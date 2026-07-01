using Onboarding.Domain.Enums;

namespace Onboarding.Application.Abstractions;

public interface IDocumentRulesService
{
    IReadOnlyList<DocumentType> GetRequiredDocuments(Segment segment);
}
