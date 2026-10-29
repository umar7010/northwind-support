using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Application.Contracts.Agents;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class RaiseTicketCommandHandlerTests
{
    private static CreateTicketRequest Request(TicketPriority? priority = null) =>
        new("Cannot log in to the portal", "Happens for every user since this morning.", 1, 3, priority);

    [Fact]
    public async Task HandleAsync_StampsReferenceStatusAndTimestamps()
    {
        var context = new TicketCommandTestContext();

        await context.RaiseTicket.HandleAsync(Request(TicketPriority.High), CancellationToken.None);

        var created = context.AddedTicket;
        Assert.NotNull(created);
        Assert.Equal("TCK-0041", created.Reference);
        Assert.Equal(TicketStatus.New, created.Status);
        Assert.Equal(TicketPriority.High, created.Priority);
        Assert.Equal(FixedClock.DefaultNow, created.CreatedAtUtc);
        Assert.Equal(FixedClock.DefaultNow, created.UpdatedAtUtc);
        context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task HandleAsync_WithoutRequestedPriority_FallsBackToMedium()
    {
        var context = new TicketCommandTestContext();

        await context.RaiseTicket.HandleAsync(Request(), CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(TicketPriority.Medium, context.AddedTicket.Priority);
    }


    [Fact]
    public async Task HandleAsync_WhenCustomerDoesNotExist_Throws()
    {
        var context = new TicketCommandTestContext();

        context.Customers
            .Setup(c => c.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportDesk.Domain.Aggregates.Customers.Customer?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => context.RaiseTicket.HandleAsync(Request(), CancellationToken.None));

        Assert.Contains("Customer", exception.Message);
        context.VerifySaved(Times.Never());
    }


    [Fact]
    public async Task HandleAsync_WhenCategoryForcesCritical_OverridesRequestedPriority()
    {
        var context = new TicketCommandTestContext();

        context.Categories
            .Setup(c => c.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category(
                "Outage",
                requiresSpecialist: false,
                forcesCriticalPriority: true));

        await context.RaiseTicket.HandleAsync(
            Request(TicketPriority.Low),
            CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(TicketPriority.Critical, context.AddedTicket.Priority);
    }


    [Fact]
    public async Task HandleAsync_ForPremiumCustomer_CalculatesCorrectSlaDeadline()
    {
        var context = new TicketCommandTestContext();

        await context.RaiseTicket.HandleAsync(
            Request(TicketPriority.High),
            CancellationToken.None);

        Assert.NotNull(context.AddedTicket);

        var expectedDueDate = FixedClock.DefaultNow.AddHours(4);

        Assert.Equal(expectedDueDate, context.AddedTicket.DueAtUtc);
    }


    [Fact]
    public async Task HandleAsync_AssignsAgentWithFewestOpenTickets()
    {
        var context = new TicketCommandTestContext();

        context.AgentQueries
            .Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentDto>
            {
            new(1, "Alex", "alex@example.com",
                true, 10, 5, Array.Empty<CategorySummaryDto>()),

            new(2, "Sarah", "sarah@example.com",
                true, 10, 2, Array.Empty<CategorySummaryDto>()),

            new(3, "John", "john@example.com",
                true, 10, 4, Array.Empty<CategorySummaryDto>())
            });

        await context.RaiseTicket.HandleAsync(
            Request(TicketPriority.Medium),
            CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(2, context.AddedTicket.AssignedAgentId);

        Assert.Null(context.AddedTicket.UnassignedReason);
    }


    [Fact]
    public async Task HandleAsync_WhenSpecialistRequired_AssignsQualifiedAgent()
    {
        var context = new TicketCommandTestContext();

        context.Categories
            .Setup(c => c.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category(
                "Billing",
                requiresSpecialist: true,
                forcesCriticalPriority: false));

        context.AgentQueries
            .Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentDto>
            {
            new(1, "Alex", "alex@example.com",
                true, 10, 1,
                Array.Empty<CategorySummaryDto>()),

            new(2, "Sarah", "sarah@example.com",
                true, 10, 4,
                new List<CategorySummaryDto>
                {
                    new(0, "Billing")
                })
            });

        await context.RaiseTicket.HandleAsync(
            Request(TicketPriority.Medium),
            CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(2, context.AddedTicket.AssignedAgentId);
    }


    [Fact]
    public async Task HandleAsync_WhenAgentIsAtCapacity_SkipsThatAgent()
    {
        var context = new TicketCommandTestContext();

        context.AgentQueries
            .Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentDto>
            {
            new(1, "Alex", "alex@example.com",
                true, 2, 2, Array.Empty<CategorySummaryDto>()),

            new(2, "Sarah", "sarah@example.com",
                true, 10, 4, Array.Empty<CategorySummaryDto>())
            });

        await context.RaiseTicket.HandleAsync(
            Request(TicketPriority.Medium),
            CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Equal(2, context.AddedTicket.AssignedAgentId);
    }


    [Fact]
    public async Task HandleAsync_WhenNoEligibleAgent_LeavesTicketUnassigned()
    {
        var context = new TicketCommandTestContext();

        context.AgentQueries
            .Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AgentDto>
            {
            new(1, "Alex", "alex@example.com",
                true, 2, 2, Array.Empty<CategorySummaryDto>()),

            new(2, "Sarah", "sarah@example.com",
                false, 10, 1, Array.Empty<CategorySummaryDto>())
            });

        await context.RaiseTicket.HandleAsync(
            Request(TicketPriority.Medium),
            CancellationToken.None);

        Assert.NotNull(context.AddedTicket);
        Assert.Null(context.AddedTicket.AssignedAgentId);

        Assert.Equal(
            "No eligible agent available",
            context.AddedTicket.UnassignedReason);
    }

}
