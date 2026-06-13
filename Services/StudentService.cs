using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TmsApi.Models;

namespace TmsApi.Services;

public class StudentService(ILogger<StudentService> logger) : IStudentService
{
    private readonly ConcurrentDictionary<string, Student> _students = new();

    public Task<IEnumerable<Student>> GetAllAsync()
    {
        logger.LogInformation("Retrieving all registered student profiles.");
        return Task.FromResult(_students.Values.AsEnumerable());
    }

    public Task<Student?> GetByIdAsync(string id)
    {
        var standardizedId = id.ToUpperInvariant();
        
        if (!_students.TryGetValue(standardizedId, out var student))
        {
            logger.LogWarning("Student lookup failed. Student with ID {StudentId} does not exist.", standardizedId);
            return Task.FromResult<Student?>(null);
        }

        logger.LogInformation("Successfully fetched profile for Student ID {StudentId}.", standardizedId);
        return Task.FromResult<Student?>(student);
    }

    public Task<Student> RegisterAsync(Student student)
    {
        var standardizedId = student.Id.ToUpperInvariant();

        if (_students.ContainsKey(standardizedId))
        {
            logger.LogWarning("Registration blocked. Student ID {StudentId} is already assigned to an active profile.", standardizedId);
            throw new System.ArgumentException($"Student with ID {standardizedId} is already registered.");
        }

        var newStudent = new Student
        {
            Id = standardizedId,
            Name = student.Name,
            Age = student.Age,
            GPA = student.GPA
        };

        _students[standardizedId] = newStudent;
        
        logger.LogInformation("Successfully registered new student identity profile. ID: {StudentId}, Name: {StudentName}.", standardizedId, student.Name);
        return Task.FromResult(newStudent);
    }

    public Task<bool> RemoveAsync(string id)
    {
        var standardizedId = id.ToUpperInvariant();
        var removed = _students.TryRemove(standardizedId, out _);

        if (!removed)
        {
            logger.LogWarning("Profile removal failed. Student ID {StudentId} was not found.", standardizedId);
            return Task.FromResult(false);
        }

        logger.LogInformation("Successfully removed student profile matching ID {StudentId}.", standardizedId);
        return Task.FromResult(true);
    }
}