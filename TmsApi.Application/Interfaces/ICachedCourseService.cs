using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface ICachedCourseService
{
    Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct);
    Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct);
    Task InvalidateCourseCacheAsync(CancellationToken ct);
}