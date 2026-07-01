using FluentAssertions;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.UnitTests.Domain;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("11.444.777/0001-61")]
    [InlineData("11444777000161")]
    public void Cnpj_Create_Should_Return_Success_When_Valid(string value)
    {
        var result = Cnpj.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("11444777000161");
    }

    [Theory]
    [InlineData("11.444.777/0001-62")]
    [InlineData("00000000000000")]
    public void Cnpj_Create_Should_Return_Failure_When_Invalid(string value)
    {
        var result = Cnpj.Create(value);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void Cpf_Create_Should_Return_Success_When_Valid(string value)
    {
        var result = Cpf.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("52998224725");
    }

    [Theory]
    [InlineData("529.982.247-24")]
    [InlineData("11111111111")]
    public void Cpf_Create_Should_Return_Failure_When_Invalid(string value)
    {
        var result = Cpf.Create(value);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("5411")]
    [InlineData("5812")]
    public void Mcc_Create_Should_Return_Success_When_Valid(string value)
    {
        var result = Mcc.Create(value);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("541")]
    [InlineData("9999")]
    [InlineData("ABCD")]
    public void Mcc_Create_Should_Return_Failure_When_Invalid(string value)
    {
        var result = Mcc.Create(value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void S3Key_Create_Should_Return_Success_When_Valid()
    {
        var proposalId = ProposalId.New();
        var result = S3Key.Create($"proposals/{proposalId.Value}/documents/{DocumentType.CnpjCard}/file.pdf");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void S3Key_Create_Should_Return_Failure_When_Invalid()
    {
        var result = S3Key.Create("documents/file.pdf");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LegalName_Create_Should_Return_Failure_When_Empty()
    {
        var result = LegalName.Create(" ");

        result.IsFailure.Should().BeTrue();
    }
}
