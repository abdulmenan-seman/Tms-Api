using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController(TmsDbContext context) : ControllerBase
{
    // -------------------------------------------------------------------------
    // EXPERIMENT A: Deferred Execution (LINQ Query Lifecycle)
    // -------------------------------------------------------------------------
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy(s => s.Name);

        Console.WriteLine(">>> STEP 3: Materializing query into a C# List (.ToList() executed)...");
        var results = orderedQuery.ToList(); // Database connection is opened and query runs here!

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");
        return Ok(results);
    }

    // -------------------------------------------------------------------------
    // EXPERIMENT B: SQL Translation Failure
    // -------------------------------------------------------------------------
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students = context.Students
                .Where(s => IsHonorRoll(s.GPA)) // Throws Exception! EF Core cannot map custom helper methods to SQL.
                .ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest(new { Message = ex.Message });
        }
    }

    // -------------------------------------------------------------------------
    // SOLUTIONS FOR EXPERIMENT B
    // -------------------------------------------------------------------------

    // Solution 1: Server-Side Evaluation (Highly Recommended)
    [HttpGet("translation-fix-server")]
    public IActionResult TestTranslationFixServer()
    {
        var students = context.Students
            .Where(s => s.GPA >= 3.5m) // Directly translatable inline expression
            .ToList();
        return Ok(students);
    }

    // Solution 2: Client-Side Evaluation
    [HttpGet("translation-fix-client")]
    public IActionResult TestTranslationFixClient()
    {
        var students = context.Students
            .AsEnumerable() // Pulls all rows into application RAM first
            .Where(s => IsHonorRoll(s.GPA)) // Safely evaluates custom C# logic locally in memory
            .ToList();
        return Ok(students);
    }
    [HttpGet("nplusone")]
    public async Task<IActionResult> TestNPlusOne(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n=== STARTING N+1 DEMONSTRATION ===");
        
        // 1. First query: Retrieve all students (1 query)
        var students = await context.Students.AsNoTracking().ToListAsync(cancellationToken);
        
        var resultList = new List<object>();
        // 2. Loop: For each student, query their enrollment count (N queries)
        foreach (var s in students)
        {
            var count = await context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == s.Id, cancellationToken);
                
            Console.WriteLine($"[N+1 Query] Student: {s.Name} has {count} enrollments");
            
            resultList.Add(new { s.Name, EnrollmentCount = count });
        }
        Console.WriteLine("=== ENDING N+1 DEMONSTRATION ===\n");
        return Ok(resultList);
    }

    // EXERCISE 7 - Part B: Fixed with Projection (Single Query)
    // -------------------------------------------------------------------------
    [HttpGet("nplusone-fixed")]
    public async Task<IActionResult> TestNPlusOneFixed(CancellationToken cancellationToken)
    {
        Console.WriteLine("\n=== STARTING FIXED SINGLE-TRIP QUERY ===");
        
        // Fix: Single query with projection (EF compiles this into a single query with sub-counts)
        var report = await context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);
        foreach (var r in report)
        {
            Console.WriteLine($"[Shaped Query] Student: {r.Name} has {r.EnrollmentCount} enrollments");
        }
        Console.WriteLine("=== ENDING FIXED SINGLE-TRIP QUERY ===\n");
        return Ok(report);
    }
    
    [HttpGet("nplusone-include")]
    public async Task<IActionResult> TestNPlusOneInclude(CancellationToken cancellationToken)
    {
        var students = await context.Students
            .AsNoTracking()
            .Include(s => s.Enrollments)
            .ToListAsync(cancellationToken);
        var report = students.Select(s => new
        {
            s.Name,
            EnrollmentCount = s.Enrollments.Count
        }).ToList();
        return Ok(report);
    }
}