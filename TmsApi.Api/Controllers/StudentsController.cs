using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/students")]
[Tags("Students")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class StudentsController(
    IStudentService studentService,
    LinkGenerator linkGenerator) : ControllerBase
{
    [HttpGet(Name = nameof(GetPagedStudents))]
    [ProducesResponseType(typeof(PagedResponse<StudentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Retrieve a paged list of students")]
    [EndpointDescription("Fetches a subset of system student records using structured filter, sort, and offset parameters.")]
    public async Task<IActionResult> GetPagedStudents([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await studentService.GetPagedAsync(request, ct);

        // --- HATEOAS STRUCTURAL PAGINATION LINKS ---
        var navigationLinks = new List<LinkDto>();

        // 1. Always append the current link (Self)
        navigationLinks.Add(new(
            "self", 
            linkGenerator.GetPathByName(HttpContext, nameof(GetPagedStudents), request)!, 
            "GET"));

        // 2. Append Previous Page link if available
        if (result.HasPrevious)
        {
            navigationLinks.Add(new(
                "prev-page", 
                linkGenerator.GetPathByName(HttpContext, nameof(GetPagedStudents), request with { Page = request.Page - 1 })!, 
                "GET"));
        }

        // 3. Append Next Page link if available
        if (result.HasNext)
        {
            navigationLinks.Add(new(
                "next-page", 
                linkGenerator.GetPathByName(HttpContext, nameof(GetPagedStudents), request with { Page = request.Page + 1 })!, 
                "GET"));
        }

        var hypermediaResponse = new
        {
            Metadata = new { result.TotalCount, result.Page, result.PageSize, result.TotalPages },
            Data = result.Items,
            Links = navigationLinks
        };

        return Ok(hypermediaResponse);
    }

    [HttpGet("{id:int}", Name = nameof(GetStudentById))]
    [ProducesResponseType(typeof(StudentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a student by ID")]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(id, ct);
        if (student is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Student Profile Not Found",
                Detail = $"No active student record discovered with Identifier {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }
        return Ok(student);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Register a new student")]
    public async Task<IActionResult> CreateStudent(CreateStudentRequest request, CancellationToken ct)
    {
        if (await studentService.RegistrationNumberExistsAsync(request.RegistrationNumber, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Registration Number Conflict",
                Detail = $"The registration number '{request.RegistrationNumber}' is already registered to an active student.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await studentService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetStudentById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
    [EndpointSummary("Update a student profile")]
    [EndpointDescription("Updates properties safely. Guards against lost mutations using database row concurrency checking tokens.")]
    public async Task<IActionResult> UpdateStudent(int id, UpdateStudentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await studentService.UpdateAsync(id, request, ct);
            if (result is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Modification Target Missing",
                    Detail = $"Cannot update student profile. Identifier {id} does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Catching the EF Core tracking mismatch and parsing it as a 412 status code
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails
            {
                Title = "Concurrency Conflict (Precondition Failed)",
                Detail = "The profile record has been modified by another processes thread since you loaded the form layout. Refresh and re-attempt update.",
                Status = StatusCodes.Status412PreconditionFailed
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Soft-delete a student")]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken ct)
    {
        var succeeded = await studentService.DeleteAsync(id, ct);
        if (!succeeded)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Deletion Target Missing",
                Detail = $"Cannot clear profile. Student Identifier {id} does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}