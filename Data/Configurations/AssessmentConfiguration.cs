using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        // 1. Map to physical table
        builder.ToTable("Assessments");

        // 2. Primary Key definition
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd(); // Auto-incremented by PostgreSQL

        // 3. Property constraints
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(150);

        // MaxScore e.g., max score of 999.99
        builder.Property(a => a.MaxScore)
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        // Weight e.g., 0.15 (15%) or 1.00 (100%)
        builder.Property(a => a.Weight)
            .HasColumnType("numeric(3,2)")
            .IsRequired();

        // 4. One-to-Many Relationship Mapping
        builder.HasOne(a => a.Course)
            .WithMany() // Assuming Course doesn't hold a explicit ICollection<Assessment>
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete: dropping a course automatically drops its assessments

        // 5. Indexing for Search Optimization
        // Indexing CourseId since we will frequently query assessments by Course ID (e.g., "Give me all quizzes for CS101")
        builder.HasIndex(a => a.CourseId);
    }
}