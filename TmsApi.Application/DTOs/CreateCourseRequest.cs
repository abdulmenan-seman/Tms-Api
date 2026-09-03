using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

public record CreateCourseRequest
{
    [Required]
    [RegularExpression(@"^[A-Z]{3}-\d{3}$", ErrorMessage = "Code must follow the pattern XXX-000 (e.g., CSE-101).")]
    public required string Code { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }

    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; init; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters.")]
    public string Category { get; init; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Schedule string cannot exceed 200 characters.")]
    public string Schedule { get; init; } = string.Empty;

    [RegularExpression(@"^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
    public string Status { get; init; } = "Active";

    [Required(ErrorMessage = "An instructor must be assigned to the course.")]
    public string InstructorId { get; init; } = string.Empty;

    [Range(1, 200, ErrorMessage = "Max capacity must be between 1 and 200.")]
    public int MaxCapacity { get; init; } = 30;
}