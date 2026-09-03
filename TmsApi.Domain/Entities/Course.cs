namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active or Inactive
    public string InstructorId { get; set; } = string.Empty;
    public int MaxCapacity { get; set; } = 30;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}