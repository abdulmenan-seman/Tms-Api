using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Infrastructure.Persistence;
using Asp.Versioning;
using TmsApi.Api.Middleware;
using TmsApi.Domain.Entities;
using FluentValidation;
using MediatR;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using TmsApi.Application.Filters;
using TmsApi.Infrastructure.Services;
using TmsApi.Application.Interfaces;
var builder = WebApplication.CreateBuilder(args);

// --- SERVICES REGISTRATION ---
builder.Services.AddControllers();
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

// Core Architecture Pipeline Wiring
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));
    

builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// PIPELINE ORDER MATTERS: Logging Behavior must wrap Validation Behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),       // L2 / Overall Expiration
        LocalCacheExpiration = TimeSpan.FromMinutes(2) // L1 Memory Expiration
    };
});
// Exception Filter Translation Wiring
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
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
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
{
    // Registers the filter type globally so the DI engine resolves the logger cleanly[cite: 4]
    options.Filters.Add<AuditLogFilter>();
});
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
});
// Configure the Versioning Services Engine
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0); 
    options.AssumeDefaultVersionWhenUnspecified = true; 
    options.ReportApiVersions = true; // Injects api-supported-versions response headers
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Format group names as v1, v2
    options.SubstituteApiVersionInUrl = true; // Auto-replace version placeholder in Scalar UI docs
});

// Enforce container self-tests during process bootstrap
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;   // Throws exception if a singleton captures a scoped service
    options.ValidateOnBuild = true;  // Triggers checking at boot rather than execution runtime
});
builder.Services.AddRateLimiter(options =>
{
    // 1. Partitioned Global Limiter based on Client Tier
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

        return tier switch
        {
            ApiKeyTier.Paid => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"paid:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 100,
                    TokensPerPeriod = 20,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),

            ApiKeyTier.Free => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"free:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 10,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }),

            _ => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: $"anon:{partitionKey}",
                factory: _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 10,
                    TokensPerPeriod = 5,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                })
        };
    });

    // 2. Concurrency Limiter for Heavy Workloads (e.g., Transcript Generation)
    options.AddConcurrencyLimiter("transcripts", opt =>
    {
        opt.PermitLimit = 5; // Max 5 active running requests
        opt.QueueLimit = 20; // Queue up to 20 additional requests
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // 3. Search Endpoint Token Bucket Limiter
    options.AddTokenBucketLimiter("search", opt =>
    {
        opt.TokenLimit = 10;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
        opt.AutoReplenishment = true;
    });

    // Custom 429 Error Response Handler with Dynamic Retry-After Header
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
        {
            retryAfter = ((int)ts.TotalSeconds).ToString();
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        }, ct);
    };
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
app.UseMiddleware<V1DeprecationMiddleware>();

app.UseExceptionHandler();
app.UseStatusCodePages(); // Transforms empty status codes (like bare 404s) into ProblemDetails JSON


// Step C: Basic protocols & routing
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
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
app.UseMiddleware<TmsApi.Api.Middleware.V1DeprecationMiddleware>();
app.MapControllers();
// After app.MapControllers() or app.UseAuthorization()

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    // var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    // await TmsApi.Persistence.DataSeeder.SeedAsync(context);
    app.MapOpenApi();
    //use swagger ui in development
    app.MapScalarApiReference(
        options =>
{
    options.WithTitle("TMS API Reference")
           .WithTheme(ScalarTheme.DeepSpace)
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

    options.AddDocument("v1", "API Version 1.0")
           .AddDocument("v2", "API Version 2.0");
}
    );  // This creates the /scalar/v1 endpoint
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