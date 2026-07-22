using Onboarding.Application.Proposals;
using Onboarding.Domain.Aggregates;

namespace Onboarding.Mappings;

public static class ProposalResponseMapper
{
    public static ProposalDetailsResponse ToDetailsResponse(this Proposal src)
    {
        var documents = src.Documents
            .Select(d => new ProposalDocumentResponse(
                d.DocumentType,
                "Uploaded",
                d.UploadedAt))
            .ToList();

        return new ProposalDetailsResponse(
            src.Id.Value,
            src.Status.ToString(),
            src.Cnpj.Value,
            src.LegalName.Value,
            src.Segment.ToString(),
            documents,
            src.CreatedAt,
            src.UpdatedAt);
    }

    public static UploadDocumentResponse ToUploadResponse(this ProposalDocument src)
    {
        return new UploadDocumentResponse(
            ((Domain.ValueObjects.UlidValue)src.Id).Value,
            src.DocumentType.ToString(),
            "Uploaded",
            src.UploadedAt);
    }
}
