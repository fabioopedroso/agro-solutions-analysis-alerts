using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces;

public interface ISensorReadingRepository
{
    Task<SensorReading?> GetByIdAsync(int id);
    Task<IEnumerable<SensorReading>> GetLast24HoursByFieldAsync(int fieldId, SensorType sensorType);
    Task AddAsync(SensorReading sensorReading);
    Task<int> SaveChangesAsync();
}
