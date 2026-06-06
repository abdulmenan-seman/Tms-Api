using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
public interface IEnrollmentService
{
    Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
    Task<EnrollmentRecord?> GetByIdAsync(string id);
    Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
    Task<bool> DeleteAsync(string id);
}
public class EnrollmentService : IEnrollmentService
{
    private readonly Dictionary<string, EnrollmentRecord> _store = new();
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(ILogger<EnrollmentService> logger)
    {
        _logger = logger;
    }

    public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
    {
        // 1. Guard against duplicate submissions before mapping
        var existing = _store.Values
            .FirstOrDefault(e => e.StudentId == studentId && e.CourseCode == courseCode); // [cite: 193, 194]

        if (existing is not null)
        {
            // WARNING: Handle unexpected but completely recoverable edge-case events cleanly
            _logger.LogWarning(
                "Duplicate enrollment attempt: Student {StudentId} already registered in Course {CourseCode} (Enrollment ID: {EnrollmentId})",
                studentId, courseCode, existing.Id); // [cite: 197, 198, 200]
            
            return Task.FromResult(existing);
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
        _store[id] = record;

        // INFORMATION: Log standard, successful milestone transactions using structured tokens
        _logger.LogInformation(
            "Successfully enrolled student {StudentId} in course {CourseCode}. Generated Record ID: {EnrollmentId}",
            studentId, courseCode, id); // [cite: 206, 207]

        return Task.FromResult(record);
    }

    public Task<EnrollmentRecord?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var record);
        
        if (record is null)
        {
            // WARNING: Resource was absent during transaction query
            _logger.LogWarning("Enrollment lookup failed. ID {EnrollmentId} not found", id); // [cite: 216]
        }

        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
    {
        IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
        return Task.FromResult(all);
    }

    public Task<bool> DeleteAsync(string id)
    {
        var removed = _store.Remove(id);
        
        if (removed)
        {
            // INFORMATION: Successful resource deletion milestone
            _logger.LogInformation("Deleted enrollment record with ID {EnrollmentId}", id); // [cite: 226]
        }
        else
        {
            // WARNING: Deletion target target entity missing or invalid
            _logger.LogWarning("Delete execution failed. Enrollment ID {EnrollmentId} did not exist", id); // [cite: 228]
        }

        return Task.FromResult(removed);
    }
}
public record EnrollmentRecord(string Id, string StudentId, string CourseCode, DateTime EnrolledAt);
