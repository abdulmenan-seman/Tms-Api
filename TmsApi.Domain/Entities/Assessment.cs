namespace TmsApi.Domain.Entities;
public class Assessment
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; } // e.g., 0.3 for 30% of final grade
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}