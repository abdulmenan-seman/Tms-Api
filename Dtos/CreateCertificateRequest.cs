using System.ComponentModel.DataAnnotations;

namespace TmsApi.Dtos;

public record CreateCertificateRequest
{
    [Required(ErrorMessage = "Serial number is mandatory.")]
    [StringLength(50, ErrorMessage = "Serial number cannot exceed 50 characters.")]
    public required string SerialNumber { get; init; }

    [Required(ErrorMessage = "Student identifier is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Student ID must be a valid positive identifier.")]
    public required int StudentId { get; init; }

    [Required(ErrorMessage = "Course identifier is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Course ID must be a valid positive identifier.")]
    public required int CourseId { get; init; }
}