using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);
    //  add addasync method to check if a student is already enrolled in a course
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct);
    // add AddAsync method to add a new enrollment
    Task AddAsync(Enrollment enrollment, CancellationToken ct);
    // add getbystudentidasync method to get all enrollments for a student
    Task<IReadOnlyList<EnrollmentResponseDto>> GetByStudentIdAsync(int studentId, CancellationToken ct);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);
}