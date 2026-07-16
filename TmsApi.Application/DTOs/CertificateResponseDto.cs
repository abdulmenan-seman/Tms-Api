namespace TmsApi.Application.DTOs;

public record CertificateResponseDto(
    int Id,
    string SerialNumber,
    DateTime IssuedAt,
    int StudentId,
    int CourseId,
    string CourseCode,
    string CourseTitle
);