using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Onboarding.ArchitectureTests;

public sealed class DependencyRuleTests
{
    private static readonly Assembly OnboardingAssembly = typeof(Program).Assembly;

    private static readonly string DomainNamespace = "Onboarding.Domain";
    private static readonly string ApplicationNamespace = "Onboarding.Application";
    private static readonly string InfrastructureNamespace = "Onboarding.Infrastructure";
    private static readonly string ControllersNamespace = "Onboarding.Controllers";
    private static readonly string MappingsNamespace = "Onboarding.Mappings";
    private static readonly string ExtensionsNamespace = "Onboarding.Extensions";
    private static readonly string WorkersNamespace = "Onboarding.Workers";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Controllers()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .Should()
            .NotHaveDependencyOn(ControllersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Mappings()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .Should()
            .NotHaveDependencyOn(MappingsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Extensions()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .Should()
            .NotHaveDependencyOn(ExtensionsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Workers()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNamespace)
            .Should()
            .NotHaveDependencyOn(WorkersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Controllers()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .Should()
            .NotHaveDependencyOn(ControllersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Extensions()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .Should()
            .NotHaveDependencyOn(ExtensionsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Workers()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .Should()
            .NotHaveDependencyOn(WorkersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(ControllersNamespace)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Workers()
    {
        var result = Types.InAssembly(OnboardingAssembly)
            .That()
            .ResideInNamespaceStartingWith(ControllersNamespace)
            .Should()
            .NotHaveDependencyOn(WorkersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
