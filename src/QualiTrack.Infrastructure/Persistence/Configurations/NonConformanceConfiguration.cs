using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualiTrack.Domain;
using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Infrastructure.Persistence.Configurations;

internal sealed class NonConformanceConfiguration : IEntityTypeConfiguration<NonConformance>
{
    public void Configure(EntityTypeBuilder<NonConformance> builder)
    {
        builder.HasKey(ncr => ncr.Id);
        builder.Property(ncr => ncr.Id).ValueGeneratedNever();

        builder.Property(ncr => ncr.Number)
            .HasConversion(number => number.Value, text => NcrNumber.Parse(text));
        builder.HasIndex(ncr => ncr.Number).IsUnique();

        builder.ComplexProperty(ncr => ncr.Details, details =>
        {
            details.Property(value => value.Title);
            details.Property(value => value.Description);
            details.Property(value => value.Location);
            details.Property(value => value.Severity).HasConversion<string>();
        });

        builder.ComplexProperty(ncr => ncr.Raised, MapStamp);

        builder.Property(ncr => ncr.Status).HasConversion<string>();
        builder.Property(ncr => ncr.InvestigatorId).HasConversion(new UserIdConverter());
        builder.Property(ncr => ncr.DueDate);
        builder.Property(ncr => ncr.RootCause);

        // Actions and history are parts of the aggregate, always loaded and saved with it.
        // A JSON array keeps them in one row and in order, which the audit trail (BR-7) needs.
        // EF maps the private lists; the public read-only views stay read-only.
        builder.Ignore(ncr => ncr.Actions);
        builder.Ignore(ncr => ncr.History);

        builder.ComplexCollection<List<CorrectiveAction>, CorrectiveAction>("_actions", actions =>
        {
            actions.ToJson("Actions");
            actions.Property(action => action.Id);
            actions.Property(action => action.Description);
            actions.ComplexProperty(action => action.Added, MapStamp);
            actions.ComplexProperty(action => action.Completed, MapStamp);
        });

        builder.ComplexCollection<List<StatusChange>, StatusChange>("_history", history =>
        {
            history.ToJson("History");
            history.Property(change => change.From).HasConversion<string>();
            history.Property(change => change.To).HasConversion<string>();
            history.ComplexProperty(change => change.Stamp, MapStamp);
            history.Property(change => change.Note);
        });
    }

    private static void MapStamp(ComplexPropertyBuilder<AuditStamp> stamp)
    {
        stamp.Property(value => value.By).HasConversion(new UserIdConverter());
        stamp.Property(value => value.At);
    }
}
