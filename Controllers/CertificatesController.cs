using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TmsApi.Dtos;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/certificates")]
[Tags("Certificates")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)] // Global catch-all
public class CertificatesController(
    ICertificateService certificateService,
    LinkGenerator linkGenerator) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCertificateById))]
    [ProducesResponseType(typeof(CertificateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a certificate by ID")]
    [EndpointDescription("Returns specific certificate details alongside operational HATEOAS discovery links.")]
    public async Task<IActionResult> GetCertificateById(int id, CancellationToken ct)
    {
        var certificate = await certificateService.GetByIdAsync(id, ct);
        if (certificate is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Certificate Not Found",
                Detail = $"No certificate structure discovered with Identifier {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // --- ENHANCE DTO WITH HATEOAS DISCOVERY LINKS ---
        var links = new List<LinkDto>
        {
            new(
                "self",
                linkGenerator.GetPathByName(HttpContext, nameof(GetCertificateById), new { id = certificate.Id })!,
                "GET"),
            new(
                "associated-course",
                linkGenerator.GetPathByAction(HttpContext, action: "GetCourseById", controller: "Courses", values: new { id = certificate.CourseId }) ?? $"/api/courses/{certificate.CourseId}",
                "GET"),
            new(
                "associated-enrollments",
                linkGenerator.GetPathByAction(HttpContext, action: "GetEnrollments", controller: "Enrollments", values: new { courseId = certificate.CourseId }) ?? $"/api/courses/{certificate.CourseId}/enrollments",
                "GET")
        };

        // Standard anonymous payload shape to ship hypermedia states transparently
        var hypermediaResponse = new
        {
            Data = certificate,
            Links = links
        };

        return Ok(hypermediaResponse);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CertificateResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Issue a new certificate")]
    [EndpointDescription("Validates and provisions a new immutable academic certificate. Rejects duplicate serial numbers.")]
    public async Task<IActionResult> IssueCertificate(CreateCertificateRequest request, CancellationToken ct)
    {
        // Business Rule Guard: Prevent resource collisions
        if (await certificateService.SerialNumberExistsAsync(request.SerialNumber, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Serial Number Conflict",
                Detail = $"A certificate with serial number '{request.SerialNumber}' has already been registered in the system.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await certificateService.CreateAsync(request, ct);
        
        return CreatedAtAction(nameof(GetCertificateById), new { id = result.Id }, result);
    }
}