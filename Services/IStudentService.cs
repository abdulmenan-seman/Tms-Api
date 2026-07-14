using TmsApi.Dtos;

namespace TmsApi.Services;

public interface IStudentService
{
    Task<StudentResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    
    // Updated from the old structural tuple to our professional generic DTO envelope shape!
    Task<PagedResponse<StudentResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct);
    
    Task<StudentResponseDto> CreateAsync(CreateStudentRequest request, CancellationToken ct);
    Task<StudentResponseDto?> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
    Task<bool> RegistrationNumberExistsAsync(string regNumber, CancellationToken ct);
}