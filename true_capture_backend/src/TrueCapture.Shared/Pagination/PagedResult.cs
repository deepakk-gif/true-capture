namespace TrueCapture.Shared.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int              Total,
    int              Page,
    int              Size)
{
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Size);
}
