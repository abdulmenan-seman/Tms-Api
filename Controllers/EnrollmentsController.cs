using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(IEnrollmentService enrollmentService) : ControllerBase
{
    // GET /api/enrollments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await enrollmentService.GetAllAsync();
        return Ok(enrollments); // Returns 200 OK with the array
    }

    // GET /api/enrollments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record) : NotFound(); // Returns 200 OK or 404 Not Found
    }
    // POST /api/enrollments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        
        // Sets status 201, populates Location header to /api/enrollments/{id}, and returns body
        return CreatedAtAction(nameof(GetById), new { id = record.Id }, record);
    }
    // DELETE /api/enrollments/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await enrollmentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound(); // Returns 204 No Content or 404 Not Found
    }
}

public record CreateEnrollmentRequest(string StudentId, string CourseCode);
