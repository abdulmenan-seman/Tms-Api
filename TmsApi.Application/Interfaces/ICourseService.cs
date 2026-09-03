using TmsApi.Application.Courses.Commands;
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<CourseResponseDto?> GetByCodeAsync(string code, CancellationToken ct);
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    Task<CourseResponseDto?> UpdateAsync(UpdateCourseCommand command, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct); // Ready for Ex 3
    Task<bool> DeleteAsync(int id, CancellationToken ct); // Ready for Ex 3
}