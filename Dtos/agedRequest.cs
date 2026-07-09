namespace TmsApi.Dtos;

public record PagedRequest
{
    private const int MaxPageSize = 50; // Single source of truth
    private int _pageSize = 20;

    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        // Restricts both bounds: value < 1 resets to default 20, value > 50 clamps to 50
        init => _pageSize = value < 1 ? 20 : value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Search { get; init; }
    public string OrderBy { get; init; } = "Title";
    public bool Descending { get; init; }
}