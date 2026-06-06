using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES REGISTRATION ---
builder.Services.AddControllers();
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

var app = builder.Build();

// --- APPLICATION REQUEST PIPELINE (STRICT MIDDLEWARE ORDER) ---

// Step A: Custom logging goes FIRST to trap and correlate all operations
app.UseMiddleware<RequestLoggingMiddleware>();

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
app.Run();