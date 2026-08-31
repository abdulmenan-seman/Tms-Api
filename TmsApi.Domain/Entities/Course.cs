namespace TmsApi.Domain.Entities;
public class Course
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public string InstructorId { get; set; } = string.Empty;
    public int MaxCapacity { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = [];
}