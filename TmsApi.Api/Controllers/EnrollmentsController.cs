using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/enrollments")] 
[Tags("Enrollments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class EnrollmentsController(
    ICourseService courseService,
    IEnrollmentService enrollmentService) : ControllerBase
{
    [HttpGet(Name = "ListCourseEnrollments")]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>),
StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List enrolments for a course")]
    public async Task<IActionResult> GetEnrollments(int courseId, CancellationToken ct)
    {
        // TODO 4: Defensive validation block. 404 if the parent course doesn't exist.[cite: 5]
        var courseExists = await courseService.GetByIdAsync(courseId, ct);
        if (courseExists is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course Not Found",
                Detail = $"Cannot retrieve enrollments because course with ID {courseId} does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Parent exists safely; execute collection projection[cite: 5]
        var enrollments = await enrollmentService.GetByCourseAsync(courseId, ct);
        return Ok(enrollments);
    }
    [HttpGet("{id:int}", Name = nameof(GetEnrollment))]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one enrolment for a course")]
    public async Task<IActionResult> GetEnrollment(int courseId, int id, CancellationToken ct)
    {
        var enrollment = await enrollmentService.GetByIdAsync(courseId, id, ct);
        return enrollment is not null ? Ok(enrollment) : NotFound();
    }

   [HttpPost]
[ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[EndpointSummary("Enrol a student in a course")]
public async Task<IActionResult> EnrollStudent(int courseId, EnrollStudentRequest request, CancellationToken ct)
{
    // 1. Course Existence Check (404)
    var course = await courseService.GetByIdAsync(courseId, ct);
    if (course is null)
    {
        return NotFound(new ProblemDetails
        {
            Title = "Course Not Found",
            Detail = $"No course entry registered under ID {courseId}.",
            Status = StatusCodes.Status404NotFound
        });
    }

    // 2. Activation Check (400)
    if (!string.Equals(course.Status, "Active", StringComparison.OrdinalIgnoreCase))
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Course Inactive",
            Detail = $"Course '{course.Title}' is currently inactive and not accepting enrollments.",
            Status = StatusCodes.Status400BadRequest
        });
    }

    // 3. Capacity Constraint Check (409)
    if (course.EnrollmentCount >= course.MaxCapacity)
    {
        return Conflict(new ProblemDetails
        {
            Title = "Course is full",
            Detail = $"Course '{course.Title}' has reached its maximum capacity of {course.MaxCapacity}.",
            Status = StatusCodes.Status409Conflict
        });
    }

    // 4. Create Enrollment (Defaults to Status: "Pending")
    var enrollment = await enrollmentService.CreateAsync(courseId, request, ct);
    return CreatedAtAction(nameof(GetEnrollment), new { courseId, id = enrollment.Id }, enrollment);
}
[HttpGet("my-enrollments")]
[Authorize]
[ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetMyEnrollments(CancellationToken ct)
{
    // Try standard name identifier, then 'sub', then 'id'
    var studentIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                         ?? User.FindFirstValue("sub") 
                         ?? User.FindFirstValue("id");

    if (string.IsNullOrEmpty(studentIdClaim))
    {
        // Return empty or unauthorized if JWT doesn't contain a user ID claim
        return Unauthorized("User ID claim not found in JWT token.");
    }

    // Convert string claim to integer ID
    if (!int.TryParse(studentIdClaim, out var studentId))
    {
        return BadRequest($"Student ID claim '{studentIdClaim}' is not a valid integer.");
    }

    var enrollments = await enrollmentService.GetByStudentIdAsync(studentId, ct);
    return Ok(enrollments);
}
}