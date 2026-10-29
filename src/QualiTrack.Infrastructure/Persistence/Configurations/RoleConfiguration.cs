using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualiTrack.Application.Abstractions;

namespace QualiTrack.Infrastructure.Persistence.Configurations;

/// <summary>
/// The three roles of PRD §4 are part of the schema, so every environment has them, not only Development.
/// Each role is named after its <see cref="Role"/> value.
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder) =>
        builder.HasData(
            Seed(Role.Inspector, "c33ee7d7-1443-4b5d-a9e3-76c95376fb7c", "cdbbc068-582f-4cd7-9258-f4d4cbad7211"),
            Seed(Role.Investigator, "c89f3d68-15b6-440d-9fe7-c98b91bfa962", "ec4145f4-63bd-4f13-89f8-1fa6601ff792"),
            Seed(Role.QaManager, "8e23cde2-7b5b-4535-adfd-3d44b37eeebf", "0cc6b840-a183-4c95-936e-45d0834a01ac"));

    // Seed data must be identical on every model build, or each new migration would rewrite these rows
    private static IdentityRole<Guid> Seed(Role role, string id, string concurrencyStamp) =>
        new()
        {
            Id = Guid.Parse(id),
            Name = role.ToString(),
            NormalizedName = role.ToString().ToUpperInvariant(),
            ConcurrencyStamp = concurrencyStamp,
        };
}
