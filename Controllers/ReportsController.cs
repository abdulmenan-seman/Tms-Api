using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(TmsDbContext context) : ControllerBase
{
    // 1. Who is enrolled in more than one course?
    [HttpGet("highly-enrolled")]
    public async Task<IActionResult> GetHighlyEnrolledStudents()
    {
        var list = await context.Students
            .Where(s => s.Enrollments.Count > 1)
            .OrderBy(s => s.Name)
            .Select(s => new { s.Name, EnrollmentCount = s.Enrollments.Count })
            .ToListAsync();

        return Ok(list);
    }

    // 2. Which courses are at capacity? (Showing remaining seats)
    [HttpGet("courses-status")]
    public async Task<IActionResult> GetCourseCapacities()
    {
        var list = await context.Courses
            .Select(c => new
            {
                c.Title,
                c.Capacity,
                EnrolledCount = c.Enrollments.Count,
                SeatsRemaining = c.Capacity - c.Enrollments.Count
            })
            .OrderByDescending(c => c.SeatsRemaining)
            .ToListAsync();

        return Ok(list);
    }
    

    // 3. What is the average GPA per course?
    [HttpGet("average-gpa-per-course")]
    public async Task<IActionResult> GetAverageGpaPerCourse()
    {
        var list = await context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();

        return Ok(list);
    }

    // 4. Which students have zero enrollments? (Show both patterns)
    
    // Approach A: Using NOT EXISTS Subquery translation
    [HttpGet("zero-enrollments-subquery")]
    public async Task<IActionResult> GetZeroEnrollmentsSubquery()
    {
        var list = await context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();

        return Ok(list);
    }

    // Approach B: Using LEFT JOIN translation
    [HttpGet("zero-enrollments-leftjoin")]
    public async Task<IActionResult> GetZeroEnrollmentsLeftJoin()
    {
        var list = await context.Students
            .LeftJoin(context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();

        return Ok(list);
    }
    //How many active students have GPA >= 3.0?
    [HttpGet("active-students-gpa")]
    public async Task<IActionResult> GetActiveStudentsWithHighGpa()
    {
        var count = await context.Students
.Where(s => s.IsActive && s.GPA >= 3.0m)
.CountAsync();
        return Ok(count);
    }
}