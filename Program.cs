using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using TmsApi.Filters;
using TmsApi.Services;
var builder = WebApplication.CreateBuilder(args);

// --- SERVICES REGISTRATION ---
builder.Services.AddControllers();
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();
// Register the DbContext with SQL Logging enabled during development
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
           .LogTo(Console.WriteLine, LogLevel.Information) // Log SQL statements to terminal output
           .EnableSensitiveDataLogging()); // Enable logging of parameter values (useful for debugging, but be cautious in production)
// Bind structural elements and assign startup schema validation checks
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")       // Extracts JSON object matching key path
    .ValidateDataAnnotations()          // Evaluates data layout model validation parameters
    .ValidateOnStart();                 // Forces immediate checking at process startup
// Add the conflicting registrations
// builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddSingleton<IStudentService, StudentService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
{
    // Registers the filter type globally so the DI engine resolves the logger cleanly[cite: 4]
    options.Filters.Add<AuditLogFilter>();
});

// Enforce container self-tests during process bootstrap
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;   // Throws exception if a singleton captures a scoped service
    options.ValidateOnBuild = true;  // Triggers checking at boot rather than execution runtime
});

var app = builder.Build();

// AUTO-SEEDER BLOCK (Saves test data at startup)
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    
    // Automatically apply any pending migrations
    context.Database.Migrate();

    if (!context.Students.Any())
    {
        Console.WriteLine("\n[SEEDER] Initializing database seeding with fresh mock records...\n");
        
        var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };
        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
        };
        context.Courses.AddRange(courses);
        
        // Save entities first to generate auto-incrementing primary key IDs
        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };
        context.Enrollments.AddRange(enrollments);
        context.SaveChanges();
        
        Console.WriteLine("[SEEDER] Database seeding completed successfully.\n");
    }
}

// --- APPLICATION REQUEST PIPELINE (STRICT MIDDLEWARE ORDER) ---

// Step A: Custom logging goes FIRST to trap and correlate all operations
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(); // Transforms empty status codes (like bare 404s) into ProblemDetails JSON


// Step C: Basic protocols & routing
app.UseHttpsRedirection();
app.UseRouting();

// Step D: Security validation blocks unauthorized traffic before endpoint mapping
app.UseAuthentication();
app.UseAuthorization();

// --- ENDPOINT CONFIGURATIONS ---
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
})).RequireAuthorization(); // Retains protected status

app.MapControllers();
// After app.MapControllers() or app.UseAuthorization()

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await TmsApi.Persistence.DataSeeder.SeedAsync(context);
    app.MapOpenApi();
    //use swagger ui in development
    app.MapScalarApiReference();  // This creates the /scalar/v1 endpoint
}
else
{
    app.UseExceptionHandler();  // Production hides Scalar automatically
}
// app.MapGet("/api/error", () =>
// {
//     throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
// });
app.Run();