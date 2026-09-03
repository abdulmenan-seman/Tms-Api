using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Instructor, Admin")]
public class CourseController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ICachedCourseService _cachedCourseService;
    private readonly ICourseService _courseService;

    public CourseController(
        ISender mediator,
        ICachedCourseService cachedCourseService,
        ICourseService courseService)
    {
        _mediator = mediator;
        _cachedCourseService = cachedCourseService;
        _courseService = courseService;
    }

    /// <summary>
    /// Retrieves all courses using HybridCache.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCourses(CancellationToken ct)
    {
        var courses = await _cachedCourseService.GetAllCoursesAsync(ct);
        return Ok(courses);
    }

    /// <summary>
    /// Retrieves a single course by unique code via HybridCache.
    /// </summary>
    [HttpGet("code/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCourseByCode(string code, CancellationToken ct)
    {
        try
        {
            var course = await _cachedCourseService.GetCourseAsync(code, ct);
            return Ok(course);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Message = $"Course with code '{code}' was not found." });
        }
    }

    /// <summary>
    /// Retrieves paginated courses directly from database service for flexible searching/sorting.
    /// </summary>
    [HttpGet("paged")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPagedCourses([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _courseService.GetCoursesAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves course details by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await _courseService.GetByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound();
        }

        return Ok(course);
    }

    /// <summary>
    /// Creates a new course and invalidates cache tags.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        if (await _courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new { Message = $"Course code '{request.Code}' already exists." });
        }

        var createdCourse = await _courseService.CreateAsync(request, ct);
        
        await _cachedCourseService.InvalidateCourseCacheAsync(ct);

        return CreatedAtAction(
            nameof(GetCourseById), 
            new { id = createdCourse.Id, version = "1.0" }, 
            createdCourse);
    }

    /// <summary>
    /// Updates an existing course using MediatR command pipeline.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseCommand command, CancellationToken ct)
    {
        if (id != command.Id)
        {
            return BadRequest(new { Message = "URL ID does not match command ID." });
        }

        var success = await _mediator.Send(command, ct);
        if (!success)
        {
            return NotFound();
        }

        await _cachedCourseService.InvalidateCourseCacheAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Deletes a course and purges cached entries.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        var course = await _courseService.GetByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound();
        }

        // Fixed: Executing actual deletion operation
        await _courseService.DeleteAsync(id, ct);
        
        await _cachedCourseService.InvalidateCourseCacheAsync(ct);

        return NoContent();
    }
}