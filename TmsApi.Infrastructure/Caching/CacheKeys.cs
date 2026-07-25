namespace TmsApi.Infrastructure.Caching;

public static class CacheKeys
{
    // Increment version (e.g., "v3") whenever the underlying DTO shape changes
    private const string SchemaVersion = "v2";

    public static string Course(string code) => $"{SchemaVersion}:course:{code}";
    public static string CoursesAll => $"{SchemaVersion}:courses:all";
    
    // Tag used for bulk cache invalidation on writes
    public const string CoursesTag = "courses";
}