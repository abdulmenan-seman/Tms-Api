using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

public record UpdateStudentRequest
{
    [Required(ErrorMessage = "Student name is mandatory.")]
    [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "GPA profile tracking value is required.")]
    [Range(0.00, 4.00, ErrorMessage = "GPA must fall strictly between 0.00 and 4.00.")]
    public required decimal GPA { get; init; }

    [Required(ErrorMessage = "IsActive status flag indicator is required.")]
    public required bool IsActive { get; init; }

    [Required(ErrorMessage = "Concurrency version key token is required to prevent stale mutations.")]
    public required uint Version { get; init; } // Required to validate Optimistic Concurrency updates
}