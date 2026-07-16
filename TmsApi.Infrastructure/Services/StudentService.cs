using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class StudentService(TmsDbContext context, ILogger<StudentService> logger) : IStudentService
{
    public async Task<StudentResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Set<Student>()
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentResponseDto(s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsActive, s.Version))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(IReadOnlyList<StudentResponseDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        var query = context.Set<Student>().AsNoTracking();

        int totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(s => s.RegistrationNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentResponseDto(s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsActive, s.Version))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<StudentResponseDto> CreateAsync(CreateStudentRequest request, CancellationToken ct)
    {
        var student = new Student
        {
            RegistrationNumber = request.RegistrationNumber,
            Name = request.Name,
            GPA = request.GPA,
            IsActive = true
        };

        context.Set<Student>().Add(student);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Enrolled new student {Id} with Registration Number {Reg}.", student.Id, student.RegistrationNumber);
        return new StudentResponseDto(student.Id, student.RegistrationNumber, student.Name, student.GPA, student.IsActive, student.Version);
    }

    public async Task<StudentResponseDto?> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken ct)
    {
        var student = await context.Set<Student>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return null;

        // Set properties
        student.Name = request.Name;
        student.GPA = request.GPA;
        student.IsActive = request.IsActive;

        // Map incoming version to original property for PostgreSQL 'xmin' comparison validation
        context.Entry(student).Property(s => s.Version).OriginalValue = request.Version;
        
        // Explicitly set shadow audit field value via change tracker API
        context.Entry(student).Property<DateTime>("LastUpdated").CurrentValue = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict triggered for Student ID {Id}.", id);
            throw; // Propagate up so controller can wrap this as a 412 or 409 error resource response
        }

        return new StudentResponseDto(student.Id, student.RegistrationNumber, student.Name, student.GPA, student.IsActive, student.Version);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var student = await context.Set<Student>().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return false;

        // Execute Soft-Delete flag inversion state mutation
        student.IsDeleted = true;
        context.Entry(student).Property<DateTime>("LastUpdated").CurrentValue = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Soft-deleted student profile configuration ID {Id}.", id);
        return true;
    }

    public async Task<bool> RegistrationNumberExistsAsync(string regNumber, CancellationToken ct)
    {
        return await context.Set<Student>()
            .AsNoTracking()
            .AnyAsync(s => s.RegistrationNumber.ToUpper() == regNumber.ToUpper(), ct);
    }
    public async Task<PagedResponse<StudentResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct)
{
    // Start with a clean queryable base
    var query = context.Set<Student>().AsNoTracking();

    // 1. Handle global Search functionality if provided by the client
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        var searchLower = request.Search.ToLower();
        query = query.Where(s => s.Name.ToLower().Contains(searchLower) || 
                                 s.RegistrationNumber.ToLower().Contains(searchLower));
    }

    // 2. Fetch total filtered count before pagination executes
    int totalCount = await query.CountAsync(ct);

    // 3. Dynamic Sorting Execution based on the Request property
    // We match against the properties, falling back to RegistrationNumber if "Id" or "Title" is passed.
    query = request.OrderBy.ToLower() switch
    {
        "name" => request.Descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
        "gpa"  => request.Descending ? query.OrderByDescending(s => s.GPA) : query.OrderBy(s => s.GPA),
        _      => request.Descending ? query.OrderByDescending(s => s.RegistrationNumber) : query.OrderBy(s => s.RegistrationNumber)
    };

    // 4. Execute pagination windows and project directly into our target DTO layout
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(s => new StudentResponseDto(s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsActive, s.Version))
        .ToListAsync(ct);

    // 5. Wrap inside your existing generic PagedResponse envelope contract!
    return new PagedResponse<StudentResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
}