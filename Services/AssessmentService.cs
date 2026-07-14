using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Data;
using TmsApi.Dtos;
using TmsApi.Entities;

namespace TmsApi.Services;

public class AssessmentService(TmsDbContext context, ILogger<AssessmentService> logger) : IAssessmentService
{
    public async Task<AssessmentResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Set<Assessment>()
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AssessmentResponseDto(
                a.Id,
                a.Title,
                a.MaxScore,
                a.Weight,
                a.CourseId,
                a.Course.Code,
                a.Course.Title
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResponse<AssessmentResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct)
    {
        var query = context.Set<Assessment>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(searchLower) || 
                                     a.Course.Title.ToLower().Contains(searchLower));
        }

        int totalCount = await query.CountAsync(ct);

        query = request.OrderBy.ToLower() switch
        {
            "title"    => request.Descending ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
            "maxscore" => request.Descending ? query.OrderByDescending(a => a.MaxScore) : query.OrderBy(a => a.MaxScore),
            "weight"   => request.Descending ? query.OrderByDescending(a => a.Weight) : query.OrderBy(a => a.Weight),
            _          => request.Descending ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AssessmentResponseDto(
                a.Id,
                a.Title,
                a.MaxScore,
                a.Weight,
                a.CourseId,
                a.Course.Code,
                a.Course.Title
            ))
            .ToListAsync(ct);

        return new PagedResponse<AssessmentResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<IEnumerable<AssessmentResponseDto>> GetByCourseIdAsync(int courseId, CancellationToken ct)
    {
        return await context.Set<Assessment>()
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => new AssessmentResponseDto(
                a.Id,
                a.Title,
                a.MaxScore,
                a.Weight,
                a.CourseId,
                a.Course.Code,
                a.Course.Title
            ))
            .ToListAsync(ct);
    }

    public async Task<AssessmentResponseDto> CreateAsync(CreateAssessmentRequest request, CancellationToken ct)
    {
        // Business Guard Rule: Sum of all weights in a course cannot exceed 1.00 (100%)
        decimal currentWeightTotal = await context.Set<Assessment>()
            .Where(a => a.CourseId == request.CourseId)
            .SumAsync(a => a.Weight, ct);

        if (currentWeightTotal + request.Weight > 1.00m)
        {
            throw new InvalidOperationException(
                $"Cannot create assessment. Adding this assessment (weight: {request.Weight}) " +
                $"would push the total course assessment weight to {currentWeightTotal + request.Weight}, " +
                $"which exceeds the allowed 1.00 (100%) threshold.");
        }

        var assessment = new Assessment
        {
            Title = request.Title,
            MaxScore = request.MaxScore,
            Weight = request.Weight,
            CourseId = request.CourseId
        };

        context.Set<Assessment>().Add(assessment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully created assessment ID {Id} for Course {CourseId}.", 
            assessment.Id, assessment.CourseId);

        return (await GetByIdAsync(assessment.Id, ct))!;
    }

    public async Task<AssessmentResponseDto?> UpdateAsync(int id, UpdateAssessmentRequest request, CancellationToken ct)
    {
        var assessment = await context.Set<Assessment>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        
        if (assessment is null) return null;

        // Business Guard Rule: Check weights excluding this actual assessment being updated
        decimal otherWeightsSum = await context.Set<Assessment>()
            .Where(a => a.CourseId == assessment.CourseId && a.Id != id)
            .SumAsync(a => a.Weight, ct);

        if (otherWeightsSum + request.Weight > 1.00m)
        {
            throw new InvalidOperationException(
                $"Cannot update assessment. This modification would make the cumulative course weight " +
                $"{otherWeightsSum + request.Weight}, exceeding the maximum allowed limit of 1.00 (100%).");
        }

        assessment.Title = request.Title;
        assessment.MaxScore = request.MaxScore;
        assessment.Weight = request.Weight;

        await context.SaveChangesAsync(ct);
        
        logger.LogInformation("Successfully updated assessment ID {Id}.", id);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var assessment = await context.Set<Assessment>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        
        if (assessment is null) return false;

        context.Set<Assessment>().Remove(assessment);
        await context.SaveChangesAsync(ct);
        
        logger.LogInformation("Deleted assessment ID {Id}.", id);
        return true;
    }
}