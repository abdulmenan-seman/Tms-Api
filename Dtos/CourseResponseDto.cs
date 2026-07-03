namespace TmsApi.Dtos;

public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount); // Calculated on demand; avoids exposing raw navigation tables