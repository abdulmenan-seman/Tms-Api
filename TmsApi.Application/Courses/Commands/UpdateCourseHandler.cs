using System.ComponentModel.DataAnnotations;
using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public record UpdateCourseCommand : IRequest<bool>
{
    [Required]
    public int Id { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; init; } = string.Empty;

    [MaxLength(200)]
    public string Schedule { get; init; } = string.Empty;

    [RegularExpression(@"^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
    public string Status { get; init; } = "Active";

    [Required]
    public string InstructorId { get; init; } = string.Empty;

    [Range(1, 200)]
    public int MaxCapacity { get; init; } = 30;
}

public class UpdateCourseHandler(
    ICourseService service,
    ICachedCourseService cachedService)
    : IRequestHandler<UpdateCourseCommand, bool>
{
    public async Task<bool> Handle(UpdateCourseCommand command, CancellationToken ct)
    {
        var updatedCourse = await service.UpdateAsync(command, ct);
        if (updatedCourse is null)
        {
            return false;
        }

        // Invalidate cached reads so callers receive fresh data immediately
        await cachedService.InvalidateCourseCacheAsync(ct);

        return true;
    }
}