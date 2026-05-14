using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using TrueCapture.Modules.Identity.Controllers;
using TrueCapture.Shared.Controllers;
using Xunit;

namespace TrueCapture.Tests.Arch;

public sealed class ArchitectureTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(AuthController).Assembly,    // TrueCapture.Modules.Identity
    ];

    [Fact]
    public void Controllers_MustExtendBaseController()
    {
        foreach (var asm in ModuleAssemblies)
        {
            var result = Types.InAssembly(asm)
                .That().HaveNameEndingWith("Controller")
                .And().AreClasses()
                .Should().Inherit(typeof(BaseController))
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "All controllers in {0} must extend BaseController. Failing: {1}",
                asm.GetName().Name,
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void Modules_MustNotReference_OtherModules()
    {
        var moduleNamespaces = ModuleAssemblies
            .Select(a => a.GetName().Name!)
            .Where(n => n.StartsWith("TrueCapture.Modules."))
            .ToArray();

        foreach (var asm in ModuleAssemblies)
        {
            var ownNs   = asm.GetName().Name!;
            var others  = moduleNamespaces.Where(n => n != ownNs).ToArray();
            if (others.Length == 0) continue;

            var result = Types.InAssembly(asm)
                .Should().NotHaveDependencyOnAny(others)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                "Module {0} must not reference any other module. Violations: {1}",
                ownNs,
                string.Join(", ", result.FailingTypeNames ?? []));
        }
    }

    [Fact]
    public void StatusFields_MustNotBeStrings_OnEntities()
    {
        // Heuristic: any property named "Status" on a domain entity must be an enum, not a string.
        foreach (var asm in ModuleAssemblies)
        {
            var entityTypes = asm.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract
                         && t.BaseType is { Name: "BaseEntity" or "TenantEntity" })
                .ToArray();

            foreach (var t in entityTypes)
            {
                var statusProp = t.GetProperty("Status");
                if (statusProp is null) continue;

                statusProp.PropertyType.IsEnum
                    .Should().BeTrue(
                        "{0}.Status should be an enum, not {1}",
                        t.FullName, statusProp.PropertyType.Name);
            }
        }
    }
}
