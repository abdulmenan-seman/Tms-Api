using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Routing;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/assessments")]
[ApiVersion("2.0")]
[Tags("Assessments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)] // Global fallback metadata
public class AssessmentsController(
    IAssessmentService assessmentService,
    LinkGenerator linkGenerator) : ControllerBase
{
    [HttpGet("{id:int}", Name = "GetAssessmentByIdV2")]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get an assessment by ID")]
    [EndpointDescription("Returns specific assessment configuration schema alongside operational HATEOAS discovery links.")]
    public async Task<IActionResult> GetAssessmentById(int id, CancellationToken ct)
    {
        var assessment = await assessmentService.GetByIdAsync(id, ct);
        if (assessment is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Assessment Not Found",
                Detail = $"No assessment configuration was found with Identifier {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // --- ENHANCE DTO WITH HATEOAS DISCOVERY LINKS ---
        var links = new List<LinkDto>
        {
            new(
                "self",
                linkGenerator.GetPathByName(HttpContext, nameof(GetAssessmentById), new { id = assessment.Id })!,
                "GET"),
            new(
                "associated-course",
                linkGenerator.GetPathByAction(HttpContext, action: "GetCourseById", controller: "Courses", values: new { id = assessment.CourseId }) ?? $"/api/courses/{assessment.CourseId}",
                "GET")
        };

        var hypermediaResponse = new
        {
            Data = assessment,
            Links = links
        };

        return Ok(hypermediaResponse);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AssessmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Retrieve a paged list of assessments")]
    [EndpointDescription("Fetches system assessments using standardized filtering, pagination, and sorting.")]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await assessmentService.GetPagedAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("course/{courseId:int}")]
    [ProducesResponseType(typeof(IEnumerable<AssessmentResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Get assessments by Course ID")]
    [EndpointDescription("Fetches all assessments structured specifically for a target course.")]
    public async Task<IActionResult> GetByCourseId(int courseId, CancellationToken ct)
    {
        var assessments = await assessmentService.GetByCourseIdAsync(courseId, ct);
        return Ok(assessments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Create an assessment")]
    [EndpointDescription("Validates and provisions a new academic evaluation criteria record. Protects the course from exceeding a 100% cumulative weight limit.")]
    public async Task<IActionResult> Create(CreateAssessmentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await assessmentService.CreateAsync(request, ct);
            return CreatedAtRoute(nameof(GetAssessmentById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            // Catch the weight limit violation and cleanly map to a 422 Unprocessable Entity
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ProblemDetails
            {
                Title = "Weight Limit Exceeded",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [EndpointSummary("Update an assessment")]
    [EndpointDescription("Updates properties of an existing assessment while recalculating cumulative weights.")]
    public async Task<IActionResult> Update(int id, UpdateAssessmentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await assessmentService.UpdateAsync(id, request, ct);
            if (result is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Modification Target Missing",
                    Detail = $"Cannot update assessment. Identifier {id} does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ProblemDetails
            {
                Title = "Weight Limit Exceeded",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Delete an assessment")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var succeeded = await assessmentService.DeleteAsync(id, ct);
        if (!succeeded)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Deletion Target Missing",
                Detail = $"Cannot remove assessment. Identifier {id} does not exist.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}