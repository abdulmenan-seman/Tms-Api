using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker
{
    private readonly IServiceScopeFactory _scopeFactory;

    // Inject the factory instead of the scoped dependency directly
    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // Generate a temporary execution container boundary using a C# 'using' block
        using var scope = _scopeFactory.CreateScope(); // [cite: 135]

        // Safely extract the scoped service instance from the local scope provider
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>(); // [cite: 137]

        // Execute batch workloads cleanly
        var current = enrollmentService.GetAllAsync().GetAwaiter().GetResult();
    }
}