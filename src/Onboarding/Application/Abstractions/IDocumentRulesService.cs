using Onboarding.Domain.Enums;
using Onboarding.Domain.Aggregates;

namespace Onboarding.Application.Abstractions;

public interface IDocumentRulesService
{
    IReadOnlyList<DocumentType> GetRequiredDocuments(Segment segment, PersonType personType);
    bool AreAllRequiredDocumentsUploaded(Proposal proposal);
}
