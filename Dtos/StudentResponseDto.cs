namespace TmsApi.Dtos;

public record StudentResponseDto(
    int Id,
    string RegistrationNumber,
    string Name,
    decimal GPA,
    bool IsActive,
    uint Version // Returned so frontend can echo it back inside Update requests
);