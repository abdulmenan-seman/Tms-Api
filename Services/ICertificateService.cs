using TmsApi.Dtos;

namespace TmsApi.Services;

public interface ICertificateService
{
    Task<CertificateResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<CertificateResponseDto> CreateAsync(CreateCertificateRequest request, CancellationToken ct);
    Task<bool> SerialNumberExistsAsync(string serialNumber, CancellationToken ct);
}