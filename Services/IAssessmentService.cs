using TmsApi.Dtos;

namespace TmsApi.Services;

public interface IAssessmentService
{
    Task<AssessmentResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResponse<AssessmentResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct);
    Task<IEnumerable<AssessmentResponseDto>> GetByCourseIdAsync(int courseId, CancellationToken ct);
    
    // Kept only the clean single-parameter signature
    Task<AssessmentResponseDto> CreateAsync(CreateAssessmentRequest request, CancellationToken ct);
    
    Task<AssessmentResponseDto?> UpdateAsync(int id, UpdateAssessmentRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}