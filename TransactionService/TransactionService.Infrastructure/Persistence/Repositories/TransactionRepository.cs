using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Persistence.Contexts;

namespace TransactionService.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
    {
        public async Task CreateAsync(Transaction transaction)
        {
            await context.Transactions.AddAsync(transaction);
        }

        public async Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default)
        {
            return await context.Transactions
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }
        

        public async Task<IReadOnlyList<Transaction>> GetAllByFromWalletIdPeerDayAsync(WalletId fromWalletId, CancellationToken cancellationToken = default)
        {
            var today = DateTime.Today;
            return await context.Transactions
                .Where(t => t.FromWalletId == fromWalletId && !t.IsDeleted
                    && t.CreatedAt >= today && t.CreatedAt < today.AddDays(1))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Transaction>> GetAllByFromWalletId(WalletId fromWalletId, CancellationToken cancellationToken = default)
        {
            return await context.Transactions
                .Where(t => t.FromWalletId == fromWalletId && !t.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            context.Transactions.Update(transaction);
            return Task.CompletedTask;
        }
    }
}
