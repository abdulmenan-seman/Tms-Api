namespace TmsApi.Entities;
public class Student
{
    public int Id { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Name { get; set; }

    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}