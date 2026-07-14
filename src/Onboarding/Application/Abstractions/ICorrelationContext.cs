namespace Onboarding.Application.Abstractions;

public interface ICorrelationContext
{
    string? CorrelationId { get; set; }
}
