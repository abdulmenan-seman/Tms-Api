using System;

namespace TmsApi.Models;

public record EnrollmentRecord(string StudentId, string CourseCode, DateTime EnrolledAt);

public class Course
{
    private int _capacity; // Manual backing field

    public required string Code { get; init; }

    public required string Title
    {
        get;
        set => field = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Title cannot be empty or whitespace.", nameof(value));
    }

    public int Capacity
    {
        get => _capacity;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be positive.");
            _capacity = value;
        }
    }

    public int EnrolledCount { get; set; }
}