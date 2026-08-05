using System;
namespace TmsApi.Domain.Entities;
public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal? Grade { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    // Bulk archive flag
    public bool IsArchived { get; set; } = false;
    //navigation properties
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}