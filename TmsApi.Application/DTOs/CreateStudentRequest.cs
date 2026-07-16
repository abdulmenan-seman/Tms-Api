using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

public record CreateStudentRequest
{
    [Required(ErrorMessage = "Registration number is mandatory.")]
    [StringLength(50, ErrorMessage = "Registration number cannot exceed 50 characters.")]
    public required string RegistrationNumber { get; init; }

    [Required(ErrorMessage = "Student name is mandatory.")]
    [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "GPA profile tracking value is required.")]
    [Range(0.00, 4.00, ErrorMessage = "GPA must fall strictly between 0.00 and 4.00.")]
    public required decimal GPA { get; init; }
}