using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
namespace TmsApi.Api.Controllers;

[Authorize(Roles = "Instructor, Admin")]
[ApiController]
[Route("api/[controller]")]
public record UpdateCourseDto(string Title);
public class CourseController : ControllerBase
{
    private readonly TmsDbContext _context;
    private readonly IAuthorizationService _authorizationService;

    public CourseController(TmsDbContext context, IAuthorizationService authorizationService)
    {
        _context = context;
        _authorizationService = authorizationService;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        var authResult = await _authorizationService.AuthorizeAsync(User, course, "CanEditCourse");
        if (!authResult.Succeeded)
        {
            return Forbid(); // 403 Forbidden when caller doesn't own the resource
        }

        course.Title = dto.Title;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}