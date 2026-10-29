
namespace SupportDesk.Application.Options;

public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    public Dictionary<string, double> BaseWindowHours { get; set; } = new();

    public double PremiumCustomerMultiplier { get; set; }

    public double MinimumWindowHours { get; set; }

    public double AtRiskThresholdPercent { get; set; }
}
