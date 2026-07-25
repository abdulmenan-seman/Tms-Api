using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.Course.Title,
                e.Course.Code,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Enrolled student {StudentId} into course {CourseId}", request.StudentId, courseId);

        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }

    public async Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByStudentIdAsync(int studentId, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.Course.Title,
                e.Course.Code,
                e.EnrolledAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.Course.Title,
                e.Course.Code,
                e.EnrolledAt))
            .ToListAsync(ct);
    }
}