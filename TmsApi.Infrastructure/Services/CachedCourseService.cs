using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseService service,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{
    public async Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        // Pass state (service, code) explicitly into factory delegate to prevent memory allocations on hot paths
        var dto = await cache.GetOrCreateAsync(
            key,
            (service, code),
            async (state, token) =>
            {
                dbHit = true; // Flag only executes during a cache MISS
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);

                var course = await state.service.GetByCodeAsync(state.code, token)
                    ?? throw new KeyNotFoundException($"Course {state.code} not found.");

                return course; // Already returns full CourseResponseDto with all 10 fields
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
        }

        return dto;
    }

    public async Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var list = await cache.GetOrCreateAsync(
            key,
            service,
            async (svc, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);

                var response = await svc.GetCoursesAsync(new PagedRequest
                {
                    Page = 1,
                    PageSize = 50
                }, token);

                return response.Items.ToList(); // Items are already full CourseResponseDto instances
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
        }

        return list;
    }

    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        logger.LogInformation("Invalidating cache tag {Tag}", CacheKeys.CoursesTag);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
    }
}