using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd(); 

        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.GPA)
            .HasColumnType("numeric(3,2)")
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property<DateTime>("LastUpdated")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.Version)
            .IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        // FIX 1: Enterprise Partial Unique Indexing
        // This ensures uniqueness ONLY applies to active (non-deleted) records!
        builder.HasIndex(s => s.RegistrationNumber)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false"); 

        // FIX 2: Explicit Parent-Child Navigation Constraint Lifecycle
        builder.HasMany(s => s.Enrollments)
            .WithOne(e => e.Student)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade); // Dropping/soft-deleting a student manages their cascading lifecycle safely
    }
}