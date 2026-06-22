using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Fixed: Brings EF Core async LINQ extensions into scope
using TmsApi.Data;
using TmsApi.Entities; // Fixed: Changed from TmsApi.Models to target database-persistence entities

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(TmsDbContext context) : ControllerBase
{
    // Fetch all courses from the database
    // GET /api/courses
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        var courses = await context.Courses.ToListAsync(cancellationToken);
        return Ok(courses); // 200 OK with list of courses
    }

    // =========================================================================
    // EXERCISE 3 - TODO 2: TOP 5 COURSES BY ENROLLMENT COUNT
    // Computes GroupBy and aggregates completely in PostgreSQL.
    // =========================================================================
    [HttpGet("top-courses")]
    public async Task<IActionResult> GetTopCourses(CancellationToken cancellationToken = default)
    {
        // GroupBy Course Title, select Count and Average GPA, order by count descending, and Take(5)
        var topCoursesList = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                CourseTitle = g.Key,
                EnrollmentCount = g.Count(), // Evaluates to SQL COUNT(*)
                AverageGPA = g.Average(e => e.Student.GPA) // Evaluates to SQL AVG()
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(5) // Translates to SQL LIMIT 5
            .ToListAsync(cancellationToken);

        return Ok(topCoursesList);
    }

    // GET /api/courses/{code}
    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken = default)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
        return course is not null ? Ok(course) : NotFound(); // 200 OK or 404 Not Found
    }

    // POST /api/courses
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Course course, CancellationToken cancellationToken = default)
    {
        // Add to DbContext safely using our persistence entity representation
        var createdCourseEntry = await context.Courses.AddAsync(course, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Emits 201 Created and configures Location header to pointing back to our Resource Endpoint: /api/courses/{code}
        return CreatedAtAction(nameof(GetByCode), new { code = createdCourseEntry.Entity.Code }, createdCourseEntry.Entity);
    }

    // DELETE /api/courses/{code}
    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code, CancellationToken cancellationToken = default)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
        if (course is null) return NotFound(); // 404 Not Found

        context.Courses.Remove(course);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent(); // 204 No Content
    }
}