using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        // 1. Explicitly define the physical table name mapping
        builder.ToTable("Certificates");

        // 2. Configure the Primary Key
        builder.HasKey(c => c.Id);

        // 3. Configure Property Constraints & Data Limits
        builder.Property(c => c.SerialNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.IssuedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP"); // Keep the database engine synchronized with the default time

        // 4. Performance Indexing
        // Senior Dev Note: Serial numbers are heavily searched and must be globally unique to prevent duplicate certifications.
        builder.HasIndex(c => c.SerialNumber)
            .IsUnique();

        // 5. Configure Relationships (Foreign Keys & Delete Behaviors)
        
        // Certificate belongs to one Student
        builder.HasOne(c => c.Student)
            .WithMany() // Assuming Student doesn't have a direct List<Certificate> collection property
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict delete: A student record with a real certificate shouldn't be deleted accidentally.

        // Certificate belongs to one Course
        builder.HasOne(c => c.Course)
            .WithMany() // Assuming Course doesn't have a direct List<Certificate> collection property
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict delete: A course with active certificates issued cannot be dropped randomly.
    }
}