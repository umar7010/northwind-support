
using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Services;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentQueries agentQueries,
    IUnitOfWork unitOfWork,
    IClock clock,
    SlaCalculator slaCalculator,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task<TicketDetailDto> HandleAsync(
        int id,
        EscalateTicketRequest request,
        CancellationToken ct)
    {

        var ticket = await tickets.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Ticket", id);

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Reason);

        var nowUtc = clock.UtcNow;
        var previousPriority = ticket.Priority;

        ticket.EscalatePriority(request.NewPriority, nowUtc);

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);

        var newDueDate = slaCalculator.GetDueDate(
            request.NewPriority,
            customer.Tier,
            nowUtc);

        ticket.SetDueDate(newDueDate, nowUtc);


        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);

        var agents = await agentQueries.GetAllAsync(ct);

        var selectedAgent = agents
            .Where(a => a.IsActive && a.OpenTicketCount < a.MaxOpenTickets)
            .Where(a => !category.RequiresSpecialist ||
                        a.Specializations.Any(s => s.Id == category.Id))
            .OrderBy(a => a.OpenTicketCount)
            .ThenBy(a => a.Id)
            .FirstOrDefault();

        if (selectedAgent is not null)
        {
            ticket.AssignTo(selectedAgent.Id, nowUtc);
            ticket.SetUnassignedReason(null, nowUtc);
        }
        else if (ticket.AssignedAgentId is null)
        {
            ticket.SetUnassignedReason("No eligible agent available", nowUtc);
        }


        // escalation logic

        var escalation = new TicketEscalation(
            ticket.Id,
            previousPriority,
            request.NewPriority,
            request.Reason,
            nowUtc);

        await tickets.AddEscalationAsync(escalation, ct);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated from {PreviousPriority} to {NewPriority}.",
            ticket.Reference,
            previousPriority,
            request.NewPriority);

        return await getTicket.HandleAsync(id, ct);


    }
}
