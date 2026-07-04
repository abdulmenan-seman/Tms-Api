using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        // Explicitly set the target table name
        builder.ToTable("Students");

        // Primary Key definition (Surrogate Key)
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .ValueGeneratedOnAdd(); // Auto-incremented by PostgreSQL

        // Natural Key Configuration (RegistrationNumber)
        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(50);

        // Core Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.GPA)
            .HasColumnType("numeric(3,2)")
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            // 1. Shadow Audit Property (does not exist in C# Student entity class)
        builder.Property<DateTime>("LastUpdated")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        // 2. Concurrency token mapped to PostgreSQL xmin system column
        builder.Property(s => s.Version)
            .IsRowVersion();
        // 3. Soft Delete Global Query Filter
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Enforce structural database-level uniqueness on the natural key
        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique();
    }
}