using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        // Explicitly set the target table name
        builder.ToTable("Courses", t => t.HasCheckConstraint("CK_Course_Capacity_NonNegative", "\"Capacity\" >= 0"));

        // Primary Key definition (Surrogate Key)
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd(); // Auto-incremented by PostgreSQL

        // Natural Key Configuration (Code)
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(20);

        // Core Properties
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);
// capacity should be a positive integer, so we can enforce that at the database level as well
        builder.Property(c => c.MaxCapacity)
            .IsRequired()
            .HasDefaultValue(0) ; // Set a default value and enforce greter than or equal to 0 constraint


        // Enforce structural database-level uniqueness on the natural key
        builder.HasIndex(c => c.Code)
            .IsUnique();
            
             builder.HasMany(c => c.Enrollments)
      .WithOne(e => e.Course)
      .HasForeignKey(e => e.CourseId)
      .OnDelete(DeleteBehavior.Restrict);
    }
}