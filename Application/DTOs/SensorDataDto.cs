namespace Application.DTOs;

public record SensorDataDto(
    int FieldId,
    string SensorType,
    double Value,
    DateTime Timestamp
);
