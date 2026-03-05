using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        // 1. Table mapping
        builder.ToTable("incidents", "public");

        // 2. Primary key
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        
        // 3. Property configurations
        builder.Property(i => i.Title).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Description).IsRequired().HasMaxLength(500);
        builder.Property(i => i.NetworkElementId).IsRequired();
        builder.Property(i => i.EngineerId);
        builder.Property(i => i.WaitingReason).HasMaxLength(500);
        builder.Property(i => i.ResolutionSummary).HasMaxLength(500);
        builder.Property(i => i.InvalidReason).HasMaxLength(500);
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();
        builder.Property(i => i.ClosedAt);

        // 4. Enum mappings
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
        
        builder.Property(x => x.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // 5. Concurrency token mapping (Postgres)
        // builder.Property(x => x.RowVersion)
        //     .HasColumnName("xmin")
        //     .IsConcurrencyToken()
        //     .ValueGeneratedOnAddOrUpdate();

        // 6. Indexes
        builder.HasIndex(x => new { x.Status, x.Severity })
            .HasDatabaseName("idx_incidents_status_severity");
        builder.HasIndex(x => x.EngineerId)
            .HasDatabaseName("idx_incidents_engineer_id");
        builder.HasIndex(x => x.NetworkElementId)
            .HasDatabaseName("idx_incidents_network_element_id");
    }
}