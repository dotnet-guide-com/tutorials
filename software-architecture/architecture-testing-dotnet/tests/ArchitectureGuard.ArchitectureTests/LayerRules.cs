using System.Reflection;
using ArchitectureGuard.Domain;
using NetArchTest.Rules;
using Xunit;
using TestResult = NetArchTest.Rules.TestResult;

namespace ArchitectureGuard.ArchitectureTests;

public sealed class LayerRules
{
    private static readonly Assembly DomainAssembly =
        typeof(DomainAssemblyMarker).Assembly;

    [Fact]
    public void Domain_must_not_depend_on_outer_layers()
    {
        Type[] selectedTypes = DomainAssembly
            .GetTypes()
            .Where(type => type.IsClass)
            .ToArray();

        Assert.NotEmpty(selectedTypes);

        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "ArchitectureGuard.Application",
                "ArchitectureGuard.Infrastructure")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            FormatFailure(result));
    }

    private static string FormatFailure(TestResult result)
    {
        if (result.IsSuccessful)
        {
            return string.Empty;
        }

        IEnumerable<string> failures =
            result.FailingTypes.Select(type =>
                $" - {type.FullName}{Environment.NewLine}" +
                $"   {type.Explanation}");

        return
            "Architecture rule failed: " +
            "Domain must remain independent of outer layers." +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                failures);
    }
}