namespace TmsApi.Application.DTOs;

public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    string Description,
    string Category,
    string Schedule,
    string Status,
    string InstructorId,
    int MaxCapacity,
    int EnrollmentCount
);

