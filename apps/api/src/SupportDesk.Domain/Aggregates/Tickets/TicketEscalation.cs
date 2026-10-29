
namespace SupportDesk.Domain.Aggregates.Tickets;

public sealed class TicketEscalation
{
    private TicketEscalation()
    {
        // Required by Entity Framework Core.
    }

    public int Id { get; private set; }

    public int TicketId { get; private set; }

    public int? EscalatedByAgentId { get; private set; }

    public TicketPriority PreviousPriority { get; private set; }

    public TicketPriority NewPriority { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTime EscalatedAtUtc { get; private set; }

    public TicketEscalation(
        int ticketId,
        TicketPriority previousPriority,
        TicketPriority newPriority,
        string reason,
        DateTime escalatedAtUtc,
        int? escalatedByAgentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        TicketId = ticketId;
        EscalatedByAgentId = escalatedByAgentId;
        PreviousPriority = previousPriority;
        NewPriority = newPriority;
        Reason = reason.Trim();
        EscalatedAtUtc = escalatedAtUtc;
    }
}
