
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Infrastructure.Data.Configurations;

public sealed class TicketEscalationConfiguration
    : IEntityTypeConfiguration<TicketEscalation>
{
    public void Configure(EntityTypeBuilder<TicketEscalation> builder)
    {
        builder.ToTable("TicketEscalations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.PreviousPriority)
            .IsRequired();

        builder.Property(e => e.NewPriority)
            .IsRequired();

        builder.Property(e => e.EscalatedAtUtc)
            .IsRequired();

        builder.Property(e => e.EscalatedByAgentId)
            .IsRequired(false);

        builder.HasIndex(e => e.TicketId);
    }
}
