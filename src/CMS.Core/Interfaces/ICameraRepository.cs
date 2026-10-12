using CMS.Core.Entities;

namespace CMS.Core.Interfaces;

public interface ICameraRepository
{
    Task<List<CameraDevice>> GetAllAsync();
    Task<CameraDevice?> GetByIdAsync(string id);
    Task<bool> AddAsync(CameraDevice camera);
    Task<bool> UpdateAsync(CameraDevice camera);
    Task<bool> DeleteAsync(string id);
}
