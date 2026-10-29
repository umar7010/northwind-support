using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

public sealed record TicketEscalationDto(
    int Id,
    TicketPriority PreviousPriority,
    TicketPriority NewPriority,
    string Reason,
    DateTime EscalatedAtUtc);