using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

// --- SERVICES REGISTRATION ---
builder.Services.AddControllers();
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();
// Bind structural elements and assign startup schema validation checks
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")       // Extracts JSON object matching key path
    .ValidateDataAnnotations()          // Evaluates data layout model validation parameters
    .ValidateOnStart();                 // Forces immediate checking at process startup
// Add the conflicting registrations
builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Enforce container self-tests during process bootstrap
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;   // Throws exception if a singleton captures a scoped service
    options.ValidateOnBuild = true;  // Triggers checking at boot rather than execution runtime
});

var app = builder.Build();

// --- APPLICATION REQUEST PIPELINE (STRICT MIDDLEWARE ORDER) ---

// Step A: Custom logging goes FIRST to trap and correlate all operations
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(); // Transforms empty status codes (like bare 404s) into ProblemDetails JSON

// Step B: Exception handler catches errors gracefully
app.UseExceptionHandler("/error");


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
    app.MapOpenApi();
    app.MapScalarApiReference();  // This creates the /scalar/v1 endpoint
}
else
{
    app.UseExceptionHandler();  // Production hides Scalar automatically
}
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});
app.Run();