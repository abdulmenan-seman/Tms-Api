using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TmsApi.Models;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
    // GET /api/courses
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var courses = await courseService.GetAllAsync();
        return Ok(courses); // 200 OK
    }

    // GET /api/courses/{code}
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var course = await courseService.GetByCodeAsync(code);
        return course is not null ? Ok(course) : NotFound(); // 200 OK or 404 Not Found
    }

    // POST /api/courses
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course course)
    {
        var createdCourse = await courseService.CreateAsync(course);
        
        // Emits 201 Created and configures Location to: /api/courses/{code}
        return CreatedAtAction(nameof(GetByCode), new { code = createdCourse.Code }, createdCourse);
    }

    // DELETE /api/courses/{code}
    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code)
    {
        var deleted = await courseService.DeleteAsync(code);
        return deleted ? NoContent() : NotFound(); // 204 No Content or 404 Not Found
    }
}