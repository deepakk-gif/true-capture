namespace TrueCapture.Shared.Data;

public abstract class BaseEntity
{
    public long      Id              { get; set; }
    public DateTime  CreatedAtUtc    { get; set; }
    public long?     CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc    { get; set; }
    public long?     UpdatedByUserId { get; set; }
    public bool      IsDeleted       { get; set; }
    public DateTime? DeletedAtUtc    { get; set; }
    public long?     DeletedByUserId { get; set; }
    public byte[]    RowVersion      { get; set; } = [];
}
