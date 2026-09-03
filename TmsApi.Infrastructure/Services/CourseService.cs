using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class CourseService(TmsDbContext context, ILogger<CourseService> logger) : ICourseService
{
    // === SESSION 1 METHODS ===
    
    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.Description,
                c.Category,
                c.Schedule,
                c.Status,
                c.InstructorId,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.Description,
                c.Category,
                c.Schedule,
                c.Status,
                c.InstructorId,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Schedule = request.Schedule,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
            InstructorId = request.InstructorId,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created course entry {Id} with Code {Code}", course.Id, course.Code);

        return (await GetByIdAsync(course.Id, ct))!;
    }

    public async Task<CourseResponseDto?> UpdateAsync(UpdateCourseCommand command, CancellationToken ct)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == command.Id, ct);
        if (course is null)
        {
            return null;
        }

        course.Title = command.Title;
        await context.SaveChangesAsync(ct);

        return await GetByIdAsync(course.Id, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return await context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);
    }

    // === SESSION 2 METHODS ===

    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        // Step 1: Initialize no-tracking IQueryable tracking tree
        var query = context.Courses.AsNoTracking();

        // Step 2: Apply case-insensitive filtering across Code, Title, and Category
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchPattern = $"%{request.Search}%";
            query = query.Where(c => EF.Functions.ILike(c.Title, searchPattern) 
                                  || EF.Functions.ILike(c.Code, searchPattern)
                                  || EF.Functions.ILike(c.Category, searchPattern));
        }

        // Step 3: Compute total count before applying paging
        var totalCount = await query.CountAsync(ct);

        // Step 4: Validate and Whitelist Sorting columns
        var allowedSortColumns = new[] { "Title", "Code", "MaxCapacity", "Category", "Status" };
        var sortColumn = allowedSortColumns.Contains(request.OrderBy, StringComparer.OrdinalIgnoreCase) 
            ? request.OrderBy 
            : "Title"; // Safe fallback

        // Dynamically apply sorting order safely
        if (string.Equals(sortColumn, "Code", StringComparison.OrdinalIgnoreCase))
        {
            query = request.Descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code);
        }
        else if (string.Equals(sortColumn, "MaxCapacity", StringComparison.OrdinalIgnoreCase))
        {
            query = request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity);
        }
        else if (string.Equals(sortColumn, "Category", StringComparison.OrdinalIgnoreCase))
        {
            query = request.Descending ? query.OrderByDescending(c => c.Category) : query.OrderBy(c => c.Category);
        }
        else if (string.Equals(sortColumn, "Status", StringComparison.OrdinalIgnoreCase))
        {
            query = request.Descending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status);
        }
        else
        {
            query = request.Descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title);
        }

        // Step 5: Apply pagination and project into the expanded DTO directly in SQL
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.Description,
                c.Category,
                c.Schedule,
                c.Status,
                c.InstructorId,
                c.MaxCapacity,
                c.Enrollments.Count
            ))
            .ToListAsync(ct);

        // Step 6: Package response
        return new PagedResponse<CourseResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
    // === SESSION 3 METHODS === implemnet delete method
    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return false;
        }

        context.Courses.Remove(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Deleted course entry {Id} with Code {Code}", course.Id, course.Code);

        return true;
    }
}