using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing; // Required for LinkGenerator mechanics
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/courses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class CoursesController(ICourseService courseService, LinkGenerator linkGenerator, ILogger<CoursesController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CourseResponseDto>), StatusCodes.Status200OK)]
[EndpointSummary("List courses with pagination")]
[EndpointDescription("Returns a paginated, optionally filtered listof TMS courses. PageSize is capped at 50.")]
   public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
  {
    var result = await courseService.GetCoursesAsync(request, ct);
    return Ok(result);
  }
    // Ensure this exact structural signature name maps down safely for the lookup token
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription("Returns course details with HATEOAS links. Returns 404 if the course does not exist.")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        // 1. Fetch data from backend service layer boundary
        var course = await courseService.GetByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound();
        }

        // 2. Map flat DTO elements onto the rich hypermedia layout format
        var response = new CourseDetailDto
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount
        };

        // 3. Build structural links array dynamically matching state conditions
        
        // State Link: Self Reference
        response.Links.Add(new LinkDto(
            "self",
            linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id = course.Id })!,
            "GET"));

        // State Link: Resource Modification State
        response.Links.Add(new LinkDto(
            "update",
            linkGenerator.GetPathByAction(HttpContext, action: "UpdateCourse", controller: "Courses", values: new { id = course.Id }) ?? $"/api/courses/{course.Id}",
            "PUT"));

        // State Link: Resource Destructuring Elimination State
        response.Links.Add(new LinkDto(
            "delete",
            linkGenerator.GetPathByAction(HttpContext, action: "DeleteCourse", controller: "Courses", values: new { id = course.Id }) ?? $"/api/courses/{course.Id}",
            "DELETE"));

        // State Link: Hierarchical Nested Child Collections Queries
        response.Links.Add(new LinkDto(
            "enrollments",
            linkGenerator.GetPathByAction(HttpContext, action: "GetEnrollments", controller: "Enrollments", values: new { courseId = course.Id }) ?? $"/api/courses/{course.Id}/enrollments",
            "GET"));

        // 4. CONDITIONAL BUSINESS STATE: Only present the enrollment link if capacity permits
        if (course.EnrollmentCount < course.MaxCapacity)
        {
            response.Links.Add(new LinkDto(
                "enroll",
                linkGenerator.GetPathByAction(HttpContext, action: "EnrollStudent", controller: "Enrollments", values: new { courseId = course.Id }) ?? $"/api/courses/{course.Id}/enrollments",
                "POST"));
        }
       else
{
    // Change from Serilog.Log.Warning to logger.LogWarning
    logger.LogWarning("Course {Code} is at full capacity ({Count}/{Max}). Omitting HATEOAS enrollment action link.", 
        course.Code, course.EnrollmentCount, course.MaxCapacity);
}

        return Ok(response);
    }

    // Stub endpoint definitions to preserve link generation routing map evaluations cleanly:
    [HttpPut("{id:int}")] public IActionResult UpdateCourse(int id) => NoContent();
    [HttpDelete("{id:int}")] public IActionResult DeleteCourse(int id) => NoContent();

    [HttpPost]
    [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.
    Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")][EndpointDescription("Creates a course with a unique code. Returns 409 if the course code already exists.")]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
   {
    if (await courseService.CodeExistsAsync(request.Code, ct))
    {
        return Conflict(new ProblemDetails
        {
            Title = "Course code already exists",
            Detail = $"A course with code '{request.Code}' is already registered.",
            Status = StatusCodes.Status409Conflict
        }); // Prevent unique index crashes
    }

    var result = await courseService.CreateAsync(request, ct);
    return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
}
}