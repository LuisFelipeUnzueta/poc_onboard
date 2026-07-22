using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Onboarding.ArchitectureTests;

public sealed class NamingConventionTests
{
    private static readonly Assembly OnboardingAssembly = typeof(Program).Assembly;

    [Fact]
    public void All_UseCases_Should_Have_UseCase_Suffix()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ImplementInterface(typeof(Application.Proposals.ICreateProposalUseCase))
            .Or()
            .ImplementInterface(typeof(Application.Proposals.IGetProposalUseCase))
            .Or()
            .ImplementInterface(typeof(Application.Proposals.IUploadDocumentUseCase))
            .Should()
            .HaveNameEndingWith("UseCase")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void All_Repositories_Should_Reside_In_Infrastructure()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.IProposalRepository))
            .Should()
            .ResideInNamespaceStartingWith("Onboarding.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void All_Domain_Events_Should_Reside_In_Domain()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ImplementInterface(typeof(Domain.Common.IDomainEvent))
            .Should()
            .ResideInNamespaceStartingWith("Onboarding.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
