namespace TmsApi.Application.DTOs;

public record CourseDetailDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
    public int MaxCapacity { get; init; }
    public int EnrollmentCount { get; init; }
    public List<LinkDto> Links { get; set; } = []; // State navigation surface
}