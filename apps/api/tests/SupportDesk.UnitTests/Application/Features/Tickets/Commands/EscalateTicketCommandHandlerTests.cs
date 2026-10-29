
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class EscalateTicketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_IncreasesPriorityAndRecalculatesSla()
    {
        var context = new TicketCommandTestContext();

        var ticket = Ticket.Raise(
            "TCK-0041",
            "Cannot log in",
            "Login is failing for all users.",
            1,
            3,
            TicketPriority.Medium,
            FixedClock.DefaultNow);

        context.ExistingTicket(ticket);

        await context.EscalateTicket.HandleAsync(
            ticket.Id,
            new EscalateTicketRequest(
                TicketPriority.High,
                "Major service disruption"),
            CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(
            context.Clock.UtcNow.AddHours(4),
            ticket.DueAtUtc);

        context.VerifySaved(Times.Once());
    }


    [Fact]
    public async Task HandleAsync_RecordsEscalationHistory()
    {
        var context = new TicketCommandTestContext();

        var ticket = Ticket.Raise(
            "TCK-0041",
            "Cannot log in",
            "Login is failing for all users.",
            1,
            3,
            TicketPriority.Medium,
            FixedClock.DefaultNow);

        context.ExistingTicket(ticket);

        TicketEscalation? savedEscalation = null;

        context.Tickets
            .Setup(t => t.AddEscalationAsync(
                It.IsAny<TicketEscalation>(),
                It.IsAny<CancellationToken>()))
            .Callback<TicketEscalation, CancellationToken>(
                (escalation, _) => savedEscalation = escalation)
            .Returns(Task.CompletedTask);

        await context.EscalateTicket.HandleAsync(
            ticket.Id,
            new EscalateTicketRequest(
                TicketPriority.High,
                "Major service disruption"),
            CancellationToken.None);

        Assert.NotNull(savedEscalation);
        Assert.Equal(TicketPriority.Medium, savedEscalation.PreviousPriority);
        Assert.Equal(TicketPriority.High, savedEscalation.NewPriority);
        Assert.Equal("Major service disruption", savedEscalation.Reason);
        Assert.Equal(context.Clock.UtcNow, savedEscalation.EscalatedAtUtc);

        context.VerifySaved(Times.Once());
    }


    [Theory]
    [InlineData(TicketPriority.Medium)]
    [InlineData(TicketPriority.Low)]
    public async Task HandleAsync_WhenPriorityDoesNotIncrease_Throws(
        TicketPriority newPriority)
    {
        var context = new TicketCommandTestContext();

        var ticket = Ticket.Raise(
            "TCK-0041",
            "Cannot log in",
            "Login is failing for all users.",
            1,
            3,
            TicketPriority.Medium,
            FixedClock.DefaultNow);

        context.ExistingTicket(ticket);

        await Assert.ThrowsAsync<SupportDesk.Domain.Exceptions.BusinessRuleViolationException>(
            () => context.EscalateTicket.HandleAsync(
                ticket.Id,
                new EscalateTicketRequest(
                    newPriority,
                    "Priority adjustment"),
                CancellationToken.None));

        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        context.VerifySaved(Times.Never());
    }


    [Fact]
    public async Task HandleAsync_WhenTicketDoesNotExist_Throws()
    {
        var context = new TicketCommandTestContext();

        await Assert.ThrowsAsync<SupportDesk.Application.Exceptions.NotFoundException>(
            () => context.EscalateTicket.HandleAsync(
                999,
                new EscalateTicketRequest(
                    TicketPriority.High,
                    "Urgent issue"),
                CancellationToken.None));

        context.VerifySaved(Times.Never());

        context.Tickets.Verify(
            t => t.AddEscalationAsync(
                It.IsAny<TicketEscalation>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }

}
