using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SensorReadingRepository : ISensorReadingRepository
{
    private readonly ApplicationDbContext _context;

    public SensorReadingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SensorReading?> GetByIdAsync(int id)
    {
        return await _context.SensorReadings
            .FirstOrDefaultAsync(sr => sr.Id == id);
    }

    public async Task<IEnumerable<SensorReading>> GetLast24HoursByFieldAsync(int fieldId, SensorType sensorType)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24);

        return await _context.SensorReadings
            .Where(sr => sr.FieldId == fieldId 
                      && sr.SensorType == sensorType 
                      && sr.Timestamp >= cutoffTime)
            .OrderByDescending(sr => sr.Timestamp)
            .ToListAsync();
    }

    public async Task AddAsync(SensorReading sensorReading)
    {
        await _context.SensorReadings.AddAsync(sensorReading);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
