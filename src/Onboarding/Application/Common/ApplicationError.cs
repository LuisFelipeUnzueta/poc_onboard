using Onboarding.Domain.Common;

namespace Onboarding.Application.Common;

public sealed record ApplicationError(string Code, string Message, int StatusCode)
{
    public static ApplicationError FromDomain(DomainError error, int statusCode) => new(error.Code, error.Message, statusCode);
}
