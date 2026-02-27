using Application.DTOs;

namespace Application.Interfaces;

public interface ISensorAnalysisService
{
    Task ProcessSensorReadingAsync(SensorDataDto sensorData);
}
