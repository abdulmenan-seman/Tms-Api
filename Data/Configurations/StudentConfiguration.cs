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

        // Enforce structural database-level uniqueness on the natural key
        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique();
    }
}