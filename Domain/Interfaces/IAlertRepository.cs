using Domain.Entities;

namespace Domain.Interfaces;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(int id);
    Task<IEnumerable<Alert>> GetActiveAlertsByFieldAsync(int fieldId);
    Task AddAsync(Alert alert);
    Task<int> SaveChangesAsync();
}
