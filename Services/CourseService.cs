using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TmsApi.Models;

namespace TmsApi.Services;

public class CourseService(ILogger<CourseService> logger) : ICourseService
{
    private readonly ConcurrentDictionary<string, Course> _courses = new();

    public Task<IEnumerable<Course>> GetAllAsync()
    {
        logger.LogInformation("Retrieving all courses from memory state storage.");
        return Task.FromResult(_courses.Values.AsEnumerable());
    }

    public Task<Course?> GetByCodeAsync(string code)
    {
        var standardizedCode = code.ToUpperInvariant();
        
        if (!_courses.TryGetValue(standardizedCode, out var course))
        {
            // Logging structured warnings for missing resources as learned in the blueprint
            logger.LogWarning("Course retrieval failed. Course with Code {CourseCode} was not found.", standardizedCode);
            return Task.FromResult<Course?>(null);
        }

        logger.LogInformation("Successfully retrieved course details for Code {CourseCode}.", standardizedCode);
        return Task.FromResult<Course?>(course);
    }

    public Task<Course> CreateAsync(Course course)
    {
        var standardizedCode = course.Code.ToUpperInvariant();
        
        if (_courses.ContainsKey(standardizedCode))
        {
            logger.LogWarning("Conflict detected! Action stopped. Course with Code {CourseCode} already exists.", standardizedCode);
            throw new System.ArgumentException($"Course with code {standardizedCode} already exists.");
        }

        var savedCourse = new Course 
        { 
            Code = standardizedCode, 
            Title = course.Title, 
            Capacity = course.Capacity,
            EnrolledCount = 0 
        };
        
        _courses[standardizedCode] = savedCourse;
        
        // Structured logging allows log aggregators to parse parameters like {CourseCode} easily
        logger.LogInformation("Successfully created new course record: {CourseCode} - {CourseTitle}.", standardizedCode, course.Title);
        return Task.FromResult(savedCourse);
    }

    public Task<bool> DeleteAsync(string code)
    {
        var standardizedCode = code.ToUpperInvariant();
        var removed = _courses.TryRemove(standardizedCode, out _);

        if (!removed)
        {
            logger.LogWarning("Course deletion failed. Target course with Code {CourseCode} did not exist.", standardizedCode);
            return Task.FromResult(false);
        }

        logger.LogInformation("Successfully deleted course with Code {CourseCode} from storage.", standardizedCode);
        return Task.FromResult(true);
    }
}