using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TmsApi.Models;

namespace TmsApi.Services;

public class StudentService : IStudentService
{
    private readonly ConcurrentDictionary<string, Student> _students = new();

    public Task<IEnumerable<Student>> GetAllAsync() => 
        Task.FromResult(_students.Values.AsEnumerable());

    public Task<Student?> GetByIdAsync(string id)
    {
        _students.TryGetValue(id.ToUpperInvariant(), out var student);
        return Task.FromResult(student);
    }

    public Task<Student> RegisterAsync(Student student)
    {
        var standardizedId = student.Id.ToUpperInvariant();
        var newStudent = new Student
        {
            Id = standardizedId,
            Name = student.Name,
            Age = student.Age,
            GPA = student.GPA
        };

        _students[standardizedId] = newStudent;
        return Task.FromResult(newStudent);
    }

    public Task<bool> RemoveAsync(string id) => 
        Task.FromResult(_students.TryRemove(id.ToUpperInvariant(), out _));
}