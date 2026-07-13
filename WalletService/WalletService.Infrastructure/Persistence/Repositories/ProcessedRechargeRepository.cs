using Microsoft.EntityFrameworkCore;
using WalletService.Application.Common.Interfaces;
using WalletService.Infrastructure.Persistence.Contexts;
using WalletService.Infrastructure.Persistence.Entities;

namespace WalletService.Infrastructure.Persistence.Repositories;

public sealed class ProcessedRechargeRepository : IProcessedRechargeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProcessedRechargeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> ExistsAsync(Guid rechargeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessedRecharges
            .AsNoTracking()
            .AnyAsync(x => x.RechargeId == rechargeId, cancellationToken);
    }

    public Task AddAsync(Guid rechargeId, CancellationToken cancellationToken = default)
    {
        var entity = ProcessedRecharge.Create(rechargeId);
        _dbContext.ProcessedRecharges.Add(entity);
        return Task.CompletedTask;
    }
}
