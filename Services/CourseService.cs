using Microsoft.EntityFrameworkCore;
using TmsApi.Data; 
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    // === SESSION 1 METHODS ===[cite: 4]
    
    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count)) // In-database projection count[cite: 3]
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created course metadata token mapping entry {Id}", course.Id);

        return (await GetByIdAsync(course.Id, ct))!;
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return await context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);
    }

    // === SESSION 2 METHODS ===[cite: 4]

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        // Step 1: Initialize no-tracking IQueryable tracking tree[cite: 4]
        var query = context.Courses.AsNoTracking();

        // Step 2: Apply case-insensitive filtering if a search parameter exists[cite: 4]
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchPattern = $"%{request.Search}%";
            query = query.Where(c => EF.Functions.ILike(c.Title, searchPattern) 
                                  || EF.Functions.ILike(c.Code, searchPattern));
        }

        // Step 3: Compute the absolute total dataset count BEFORE applying pagination windows[cite: 4]
        var totalCount = await query.CountAsync(ct);

        // Step 4: Validate and Whitelist Sorting columns to prevent execution injection[cite: 4]
        var allowedSortColumns = new[] { "Title", "Code", "MaxCapacity" };
        var sortColumn = allowedSortColumns.Contains(request.OrderBy, StringComparer.OrdinalIgnoreCase) 
            ? request.OrderBy 
            : "Title"; // Fallback to safe default column[cite: 4]

        // Dynamically apply sorting order safely[cite: 4]
        if (string.Equals(sortColumn, "Code", StringComparison.OrdinalIgnoreCase))
        {
            query = request.Descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code);
        }
        else if (string.Equals(sortColumn, "MaxCapacity", StringComparison.OrdinalIgnoreCase))
        {
            query = request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity);
        }
        else
        {
            query = request.Descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title);
        }

        // Step 5: Page the results and project directly into the DTO within the SQL execution tree[cite: 4]
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
            .ToListAsync(ct);

        // Step 6: Package everything into the unified response object[cite: 4]
        return new PagedResponse<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}