
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

public sealed record EscalateTicketRequest(
    TicketPriority NewPriority,
    string Reason);
