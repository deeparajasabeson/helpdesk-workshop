using System.Reflection;
using Helpdesk.Tickets.Application.Abstractions;
using NetArchTest.Rules;

namespace Helpdesk.ArchitectureTests;

/// <summary>One test per rule in docs/architecture-rules.md.</summary>
public class ArchitectureRulesTests
{
    private const string SharedKernel = "Helpdesk.SharedKernel";
    private const string Domain = "Helpdesk.Tickets.Domain";
    private const string Infrastructure = "Helpdesk.Tickets.Infrastructure";
    private const string Presentation = "Helpdesk.Tickets.Presentation";
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";

    private static readonly Assembly SharedKernelAssembly = typeof(SharedKernel.Result).Assembly;
    private static readonly Assembly DomainAssembly = typeof(Tickets.Domain.Ticket).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICommand<>).Assembly;
    private static readonly Assembly InfrastructureAssembly =
        typeof(Tickets.Infrastructure.TicketsDbContext).Assembly;
    private static readonly Assembly PresentationAssembly =
        typeof(Tickets.Presentation.TicketEndpoints).Assembly;

    // Rule 1
    [Fact]
    public void Tickets_Domain_may_depend_only_on_SharedKernel_and_System()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespaceStartingWith(Domain)
            .Should().OnlyHaveDependenciesOn(Domain, SharedKernel, "System")
            .GetResult();

        AssertPassed(result, "Tickets.Domain types depend on something other than SharedKernel and System");
    }

    // Rule 2
    [Fact]
    public void Tickets_Domain_must_not_reference_EntityFrameworkCore_anywhere()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn(EntityFrameworkCore)
            .GetResult();
        AssertPassed(result, "Tickets.Domain types depend on Microsoft.EntityFrameworkCore");

        var efAssemblies = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name => name.StartsWith(EntityFrameworkCore, StringComparison.Ordinal))
            .ToList();
        Assert.True(efAssemblies.Count == 0,
            "Tickets.Domain references EF Core assemblies: " + string.Join(", ", efAssemblies));
    }

    // Rule 3
    [Fact]
    public void Tickets_Application_must_not_depend_on_Infrastructure_or_Presentation()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOnAny(Infrastructure, Presentation)
            .GetResult();

        AssertPassed(result, "Tickets.Application types depend on Infrastructure or Presentation");
    }

    // Rule 4
    [Fact]
    public void Command_and_query_handlers_must_be_internal()
    {
        var handlers = Types.InAssemblies(TicketsAssemblies)
            .That().ImplementInterface(typeof(ICommandHandler<,>))
            .Or().ImplementInterface(typeof(IQueryHandler<,>));

        // Guard against a vacuous pass if the interface match ever stops finding handlers.
        Assert.NotEmpty(handlers.GetTypes());

        var result = handlers.Should().NotBePublic().GetResult();

        AssertPassed(result, "Command or query handlers are public");
    }

    // Rule 5
    [Fact]
    public void No_type_outside_Helpdesk_Tickets_may_depend_on_Tickets_Infrastructure()
    {
        // Helpdesk.Host is the composition root and the one allowed exception, so it is not scanned.
        var result = Types.InAssemblies([SharedKernelAssembly, .. TicketsAssemblies])
            .That().DoNotResideInNamespaceStartingWith("Helpdesk.Tickets")
            .ShouldNot().HaveDependencyOn(Infrastructure)
            .GetResult();

        AssertPassed(result, "Types outside Helpdesk.Tickets.* depend on Tickets.Infrastructure");
    }

    private static Assembly[] TicketsAssemblies =>
        [DomainAssembly, ApplicationAssembly, InfrastructureAssembly, PresentationAssembly];

    private static void AssertPassed(TestResult result, string violation)
    {
        var offenders = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful,
            $"{violation}:{Environment.NewLine}  " + string.Join(Environment.NewLine + "  ", offenders));
    }
}
