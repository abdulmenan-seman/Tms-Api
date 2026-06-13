using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TmsApi.Models;

namespace TmsApi.Services;

public class CourseService : ICourseService
{
    private readonly ConcurrentDictionary<string, Course> _courses = new();

    public Task<IEnumerable<Course>> GetAllAsync() => 
        Task.FromResult(_courses.Values.AsEnumerable());

    public Task<Course?> GetByCodeAsync(string code)
    {
        _courses.TryGetValue(code.ToUpperInvariant(), out var course);
        return Task.FromResult(course);
    }

    public Task<Course> CreateAsync(Course course)
    {
        var standardizedCode = course.Code.ToUpperInvariant();
        var savedCourse = new Course 
        { 
            Code = standardizedCode, 
            Title = course.Title, 
            Capacity = course.Capacity,
            EnrolledCount = 0 
        };
        
        _courses[standardizedCode] = savedCourse;
        return Task.FromResult(savedCourse);
    }

    public Task<bool> DeleteAsync(string code) => 
        Task.FromResult(_courses.TryRemove(code.ToUpperInvariant(), out _));
}