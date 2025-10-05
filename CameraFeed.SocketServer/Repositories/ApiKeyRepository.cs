using CameraFeed.SocketServer.Data;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.SocketServer.Repositories;

public interface IApiKeyRepository
{
    Task AddAsync(ApiKeyEntity apiKeyEntity);
    Task<bool> DeleteAsync(int id);
    Task SaveChangesAsync();
}

public class ApiKeyRepository(ApiKeyDbContext context) : IApiKeyRepository
{
    private readonly ApiKeyDbContext _context = context;
    private readonly DbSet<ApiKeyEntity> _apiKeys = context.ApiKeys;

    public async Task AddAsync(ApiKeyEntity apiKeyEntity)
    {
        await _apiKeys.AddAsync(apiKeyEntity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affectedRows = await _apiKeys.Where(k => k.Id == id).ExecuteDeleteAsync();
        return affectedRows > 0;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
