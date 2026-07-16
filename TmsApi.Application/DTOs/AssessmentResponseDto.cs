namespace TmsApi.Application.DTOs;

public record AssessmentResponseDto(
    int Id,
    string Title,
    decimal MaxScore,
    decimal Weight,
    int CourseId,
    string CourseCode,
    string CourseTitle
);