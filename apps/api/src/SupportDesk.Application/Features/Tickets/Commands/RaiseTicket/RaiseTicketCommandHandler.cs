using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Application.Services;
using Microsoft.Extensions.Options;
using SupportDesk.Application.Options;
using SupportDesk.Domain.Aggregates.Categories;

namespace SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;

/// <summary>
/// Raises a ticket for a customer.
/// </summary>
public sealed class RaiseTicketCommandHandler(
    ITicketRepository tickets,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentQueries agentQueries,
    IUnitOfWork unitOfWork,
    IClock clock,
    SlaCalculator slaCalculator,
    GetTicketQueryHandler getTicket,
    ILogger<RaiseTicketCommandHandler> logger)
{
    /// <exception cref="NotFoundException">The customer or the category does not exist.</exception>
    public async Task<TicketDetailDto> HandleAsync(CreateTicketRequest request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct);

        if (customer is null)
        {
            throw new NotFoundException("Customer", request.CustomerId);
        }

        var category = await categories.GetByIdAsync(request.CategoryId, ct);

        if (category is null)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        var priority = category.ForcesCriticalPriority
            ? TicketPriority.Critical
            : request.RequestedPriority ?? TicketPriority.Medium;

        // The response window is still worked out by hand by the team lead, so a new ticket
        // starts with no due date and no owner.
        var ticket = Ticket.Raise(
            await tickets.NextReferenceAsync(ct),
            request.Title,
            request.Description,
            request.CustomerId,
            request.CategoryId,
            priority,
            clock.UtcNow
        );

        var nowUtc = clock.UtcNow;

        var dueAtUtc = slaCalculator.GetDueDate(
            priority,
            customer.Tier,
            nowUtc);

        ticket.SetDueDate(dueAtUtc, nowUtc);


        var agents = await agentQueries.GetAllAsync(ct);

        var selectedAgent = agents
            .Where(agent => agent.IsActive)
            .Where(agent => agent.OpenTicketCount < agent.MaxOpenTickets)
            .Where(agent =>
                !category.RequiresSpecialist ||
                agent.Specializations.Any(s => s.Id == category.Id))
            .OrderBy(agent => agent.OpenTicketCount)
            .ThenBy(agent => agent.Id)
            .FirstOrDefault();


        if (selectedAgent is not null)
        {
            ticket.AssignTo(selectedAgent.Id, nowUtc);
            ticket.SetUnassignedReason(null, nowUtc);
        }
        else
        {
            ticket.SetUnassignedReason(
                "No eligible agent available",
                nowUtc);
        }



        await tickets.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} raised for customer {CustomerId} in category {CategoryId}.",
            ticket.Reference, ticket.CustomerId, ticket.CategoryId);

        return await getTicket.HandleAsync(ticket.Id, ct);
    }
}
