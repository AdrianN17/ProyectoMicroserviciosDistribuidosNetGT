using Microsoft.EntityFrameworkCore;
using WalletService.Application.Common.Interfaces;
using WalletService.Infrastructure.Persistence.Contexts;
using WalletService.Infrastructure.Persistence.Entities;

namespace WalletService.Infrastructure.Persistence.Repositories;

public sealed class ProcessedTransactionRepository : IProcessedTransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProcessedTransactionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> ExistsAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProcessedTransactions
            .AsNoTracking()
            .AnyAsync(x => x.TransactionId == transactionId, cancellationToken);
    }

    public Task AddAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        var entity = ProcessedTransaction.Create(transactionId);
        _dbContext.ProcessedTransactions.Add(entity);
        return Task.CompletedTask;
    }
}

