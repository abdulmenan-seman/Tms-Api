using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    // =========================================================================
    // EXERCISE 3 - TODO 1: SERVER-SIDE PAGINATION (LIMIT / OFFSET IN SQL)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> GetPagedStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Enforce parameter limits
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        // Perform constant-cost counting on the PostgreSQL engine
        int totalRecords = await context.Students.CountAsync(cancellationToken);
        int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        // Execute stable sorting, offsets, and limits directly inside PostgreSQL
        var items = await context.Students
            .OrderBy(s => s.Name)               // Rule: Always OrderBy before Skip/Take
            .ThenBy(s => s.Id)                  // Secondary sort to guarantee deterministic order across pages
            .Skip((page - 1) * pageSize)        // Skip translates directly to SQL OFFSET
            .Take(pageSize)                     // Take translates directly to SQL LIMIT
            .Select(s => new
            {
                s.Id,
                s.RegistrationNumber,
                s.Name,
                s.GPA,
                s.IsActive
            })
            .ToListAsync(cancellationToken);    // Query is materialized and sent to database here

        return Ok(new
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalRecords = totalRecords,
            HasPrevious = page > 1,
            HasNext = page < totalPages,
            Data = items
        });
    }
    // GET /api/students/{iauto-generated-id-or-reg-number} - Flexible endpoint to fetch by either primary key or registration number
    [HttpGet("{identifier}")]
    public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken cancellationToken = default)
    {
        // Attempt to parse identifier as integer ID first
        if (int.TryParse(identifier, out int id))
        {
            var studentById = await context.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (studentById is not null) return Ok(studentById);
        }

        // If parsing fails, treat identifier as registration number
        var studentByReg = await context.Students.FirstOrDefaultAsync(s => s.RegistrationNumber == identifier, cancellationToken);
        return studentByReg is not null ? Ok(studentByReg) : NotFound();
    }
    // POST /api/students - Create a new student record safely
    // =========================================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Student student, CancellationToken cancellationToken = default)
    {
        // // 1. Validate payload basics
        // if (string.IsNullOrWhiteSpace(student.RegistrationNumber) || string.IsNullOrWhiteSpace(student.Name))
        // {
        //     return BadRequest(new { Message = "Registration number and Name are required fields." });
        // }

        // // 2. Guard against duplicate natural keys (prevents uncaught Postgres Unique Index Violations)
        // bool exists = await context.Students.AnyAsync(
        //     s => s.RegistrationNumber == student.RegistrationNumber, 
        //     cancellationToken);

        // if (exists)
        // {
        //     return Conflict(new { Message = $"A student with registration number '{student.RegistrationNumber}' already exists." });
        // }

        // 3. Clear safety parameters to prevent Overposting vulnerabilities
        student.Id = 0; // Ensures PostgreSQL ignores any client-supplied integer surrogate ID
        student.Enrollments = new List<Enrollment>(); // Clear any nested client-supplied arrays

        // 4. EF Core best practice: Use synchronous .Add() to register state with tracker
        context.Students.Add(student);
        
        // 5. Commit state asynchronously to PostgreSQL
        await context.SaveChangesAsync(cancellationToken);

        // Emits 201 Created and configures Location header pointing back to our resource endpoint
        return CreatedAtAction(
            nameof(GetByIdentifier), 
            new { identifier = student.Id }, 
            student);
    }
}