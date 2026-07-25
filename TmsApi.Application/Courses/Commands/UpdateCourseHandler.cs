using System.Threading;
using System.Threading.Tasks;
using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Courses.Commands;

public record UpdateCourseCommand(int Id, string Title) : IRequest<bool>;

public class UpdateCourseHandler(
    ICourseService service,
    ICachedCourseService cachedService)
    : IRequestHandler<UpdateCourseCommand, bool>
{
    public async Task<bool> Handle(UpdateCourseCommand command, CancellationToken ct)
    {
        await service.UpdateAsync(command, ct);
        
        // Invalidate cached reads so callers receive fresh data immediately
        await cachedService.InvalidateCourseCacheAsync(ct);
        
        return true;
    }
}