using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Models;
using TmsApi.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(IStudentService studentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await studentService.GetAllAsync();
        return Ok(students); // 200 OK [cite: 52]
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var student = await studentService.GetByIdAsync(id);
        return student is not null ? Ok(student) : NotFound(); // 200 OK or 404 Not Found [cite: 58]
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] Student student)
    {
        var createdStudent = await studentService.RegisterAsync(student);
        
       // Emits 201 Created and configures Location to: /api/students/{id} 
        return CreatedAtAction(nameof(GetById), new { id = createdStudent.Id }, createdStudent);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await studentService.RemoveAsync(id);
        return deleted ? NoContent() : NotFound(); // 204 No Content or 404 Not Found [cite: 81, 82]
    }
}