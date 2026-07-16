using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Services;

public class CertificateService(TmsDbContext context, ILogger<CertificateService> logger) : ICertificateService
{
    public async Task<CertificateResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Set<Certificate>()
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CertificateResponseDto(
                c.Id,
                c.SerialNumber,
                c.IssuedAt,
                c.StudentId,
                c.CourseId,
                c.Course.Code,  // In-database JOIN optimization
                c.Course.Title // In-database JOIN optimization
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CertificateResponseDto> CreateAsync(CreateCertificateRequest request, CancellationToken ct)
    {
        var certificate = new Certificate
        {
            SerialNumber = request.SerialNumber,
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            IssuedAt = DateTime.UtcNow
        };

        context.Set<Certificate>().Add(certificate);
        await context.SaveChangesAsync(ct);
        
        logger.LogInformation("Successfully issued certificate {Id} with Serial Number {SerialNumber}", 
            certificate.Id, certificate.SerialNumber);

        // Re-fetch via GetByIdAsync to safely populate the complex DTO response shape
        return (await GetByIdAsync(certificate.Id, ct))!;
    }

    public async Task<bool> SerialNumberExistsAsync(string serialNumber, CancellationToken ct)
    {
        return await context.Set<Certificate>()
            .AsNoTracking()
            .AnyAsync(c => c.SerialNumber.ToUpper() == serialNumber.ToUpper(), ct);
    }
}