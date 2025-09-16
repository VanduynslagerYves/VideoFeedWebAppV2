using CameraFeed.Processor.Data;
using CameraFeed.Processor.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CameraFeed.Processor.Repositories;

public interface IWorkerRepository
{
    Task<List<WorkerDbModel>> GetAllWorkersAsync();
    Task<List<WorkerDbModel>> GetEnabledWorkersAsync();
    Task SaveChangesAsync();
}

public class WorkerRepository(CamDbContext context) : IWorkerRepository
{
    private readonly CamDbContext _context = context;
    private readonly DbSet<WorkerDbModel> _workerRecords = context.WorkerRecords;

    /// <summary>
    /// Returns all WorkerRecord entities including their Resolution.
    /// Note: 'async' and 'await' are omitted because this method simply forwards the asynchronous result
    /// of ToListAsync(). This avoids unnecessary overhead from the async state machine.
    /// </summary>
    public Task<List<WorkerDbModel>> GetAllWorkersAsync()
    {
        return _workerRecords.Include(w => w.Resolution).ToListAsync();
    }

    /// <summary>
    /// Returns only enabled WorkerRecord entities.
    /// Note: 'async' and 'await' are used here because we need to process the result
    /// of GetAllWorkersAsync() before returning.
    /// </summary>
    public async Task<List<WorkerDbModel>> GetEnabledWorkersAsync()
    {
        var allWorkers = await GetAllWorkersAsync();
        return [.. allWorkers.Where(t => t.Enabled)];
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
