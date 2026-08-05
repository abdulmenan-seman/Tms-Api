using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentsController(IMediator mediator, TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var enrollments = await context.Enrollments
            .AsNoTracking()
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.Course.Title,
                e.Course.Code,
                e.EnrolledAt,
                e.Status))
            .ToListAsync(ct);

        return Ok(enrollments);
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(EnrollStudentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),
            onFailure: error =>
            {
                var status = error.Code switch
                {
                    "course_not_found" => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled" => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message,
                    type: $"https://tms.local/errors/{error.Code}");
            });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var enrollment = await context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (enrollment is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment Not Found",
                Detail = $"No enrollment found with ID {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        enrollment.Status = "Approved";
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
}