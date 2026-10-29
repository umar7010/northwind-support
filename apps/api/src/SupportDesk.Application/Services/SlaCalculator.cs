
using Microsoft.Extensions.Options;
using SupportDesk.Application.Options;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Services;

public sealed class SlaCalculator(IOptions<SlaOptions> options)
{
    public TimeSpan GetWindow(TicketPriority priority, CustomerTier tier)
    {
        var settings = options.Value;

        var hours = settings.BaseWindowHours[priority.ToString()];

        if (tier == CustomerTier.Premium)
        {
            hours *= settings.PremiumCustomerMultiplier;
        }

        hours = Math.Max(hours, settings.MinimumWindowHours);

        return TimeSpan.FromHours(hours);
    }

    public DateTime GetDueDate(
        TicketPriority priority,
        CustomerTier tier,
        DateTime startUtc)
    {
        return startUtc.Add(GetWindow(priority, tier));
    }
}
