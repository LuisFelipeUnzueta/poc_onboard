namespace Onboarding.Controllers.Requests;

public sealed class UploadDocumentFormRequest
{
    public string DocumentType { get; init; } = string.Empty;
    public IFormFile? File { get; init; }
}
