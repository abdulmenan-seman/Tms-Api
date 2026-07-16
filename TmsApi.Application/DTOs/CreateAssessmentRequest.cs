using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

public record CreateAssessmentRequest
{
    [Required(ErrorMessage = "Assessment title is mandatory.")]
    [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
    public required string Title { get; init; }

    [Required(ErrorMessage = "Maximum possible score is required.")]
    [Range(0, 99.99, ErrorMessage = "Max score must be between 0 and 99.99.")]
    public required decimal MaxScore { get; init; }

    [Required(ErrorMessage = "Weight proportion is required.")]
    [Range(0.01, 1.00, ErrorMessage = "Weight must be between 0.01 (1%) and 1.00 (100%).")]
    public required decimal Weight { get; init; }

    [Required(ErrorMessage = "Associated Course identifier is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Course ID must be a valid positive integer.")]
    public required int CourseId { get; init; }
}