using Domain.Enums;

namespace Domain.Entities;

public class Alert
{
    public int Id { get; set; }
    public int FieldId { get; set; }
    public AlertType Type { get; set; }
    public AlertStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
