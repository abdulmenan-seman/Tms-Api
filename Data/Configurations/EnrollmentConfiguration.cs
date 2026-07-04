using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

namespace TmsApi.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        // Explicitly set the target table name
        builder.ToTable("Enrollments");

        // Primary Key definition
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Core Properties
        builder.Property(e => e.Grade)
            .HasColumnType("numeric(3,2)")
            .IsRequired(false); // Nullable grade since student might be currently enrolled

        builder.Property(e => e.EnrolledAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // =========================================================================
        // RELATIONSHIP CONFIGURATIONS & EXPLICIT FOREIGN KEYS
        // =========================================================================

        // One Student has many Enrollments
        builder.HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade deletes enrollments if student record is deleted

        // One Course has many Enrollments
        // Senior Design Choice: Prevent Course deletion if there are active student enrollment records
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}