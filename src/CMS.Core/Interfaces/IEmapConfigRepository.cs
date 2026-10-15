using CMS.Core.Entities;

namespace CMS.Core.Interfaces;

public interface IEmapConfigRepository
{
    Task<EmapConfig> GetConfigAsync();
    Task<bool> SaveConfigAsync(EmapConfig config);
}
