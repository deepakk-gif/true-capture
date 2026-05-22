namespace TrueCapture.Modules.Social.Services;

/// <summary>
/// Simple keyset pagination by descending <c>Id</c>. The cursor is just the last
/// row's id; the next page is rows with <c>Id &lt; cursor</c>.
/// </summary>
internal static class Paging
{
    public const int PageSize = 20;

    public static long? DecodeCursor(string? cursor)
        => long.TryParse(cursor, out var id) ? id : null;
}
