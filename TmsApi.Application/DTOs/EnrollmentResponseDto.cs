namespace TmsApi.Application.DTOs;

public record EnrollmentResponseDto(int Id, int CourseId, int StudentId, string CourseTitle, string CourseCode, DateTime EnrolledAt);