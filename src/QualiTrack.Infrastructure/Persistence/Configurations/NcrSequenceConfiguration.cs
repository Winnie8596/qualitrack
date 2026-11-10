using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QualiTrack.Infrastructure.Persistence.Configurations;

internal sealed class NcrSequenceConfiguration : IEntityTypeConfiguration<NcrSequence>
{
    public void Configure(EntityTypeBuilder<NcrSequence> builder)
    {
        builder.ToTable(NcrSequence.Table);
        builder.HasKey(sequence => sequence.Year);
        builder.Property(sequence => sequence.Year).HasColumnName(NcrSequence.YearColumn).ValueGeneratedNever();
        builder.Property(sequence => sequence.LastSequence).HasColumnName(NcrSequence.LastSequenceColumn);
    }
}
