using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Application.Common;
using Onboarding.Application.Proposals;

namespace Onboarding.Controllers;

[ApiController]
[Route("api/proposals")]
public sealed class ProposalsController(
    ICreateProposalUseCase createProposalUseCase,
    IGetProposalUseCase getProposalUseCase) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost]
    [ProducesResponseType<CreateProposalResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateProposalRequest request,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey)
            || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problem(new ApplicationError(
                "VALIDATION_ERROR",
                "Idempotency-Key header is required.",
                StatusCodes.Status400BadRequest));
        }

        var result = await createProposalUseCase.ExecuteAsync(
            request,
            idempotencyKey.ToString(),
            ComputeHash(request),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error!);
        }

        if (result.IdempotencyReplayed)
        {
            Response.Headers["Idempotency-Replayed"] = "true";
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return CreatedAtAction(nameof(GetByIdAsync), new { proposalId = result.Value!.ProposalId }, result.Value);
    }

    [HttpGet("{proposalId}")]
    [ProducesResponseType<ProposalDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(string proposalId, CancellationToken cancellationToken)
    {
        var result = await getProposalUseCase.ExecuteAsync(proposalId, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error!);
    }

    private ObjectResult Problem(ApplicationError error)
    {
        var problemDetails = new ProblemDetails
        {
            Status = error.StatusCode,
            Title = error.Code,
            Detail = error.Message,
            Type = $"https://httpstatuses.com/{error.StatusCode}",
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = error.Code;

        return StatusCode(error.StatusCode, problemDetails);
    }

    private static string ComputeHash(CreateProposalRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
