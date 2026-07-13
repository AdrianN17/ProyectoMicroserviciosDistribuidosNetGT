using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Persistence.Contexts;

namespace TransactionService.Infrastructure.Persistence.Repositories
{
    public class RechargeRepository : IRechargeRepository
    {
        private readonly ApplicationDbContext _context;

        public RechargeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Recharge recharge)
        {
            await _context.Recharges.AddAsync(recharge);
        }

        public async Task<Recharge?> GetByIdAsync(RechargeId id, CancellationToken cancellationToken = default)
        {
            return await _context.Recharges
                .Where(r => r.Id == id && !r.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Recharge>> GetAllByFromWalletId(WalletId walletId, CancellationToken cancellationToken = default)
        {
            return await _context.Recharges
                .Where(r => r.WalletId == walletId && !r.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateAsync(Recharge recharge, CancellationToken cancellationToken = default)
        {
            _context.Recharges.Update(recharge);
        }
        
        
    }
}