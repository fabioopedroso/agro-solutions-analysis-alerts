using Domain.Enums;

namespace Domain.Entities;

public class SensorReading
{
    public int Id { get; set; }
    public int FieldId { get; set; }
    public SensorType SensorType { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime ProcessedAt { get; set; }
}
