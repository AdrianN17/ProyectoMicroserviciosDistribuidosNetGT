using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TransactionService.Infrastructure.Persistence.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.Ignore(t => t.DomainEvents);

            builder.Property(t => t.Id)
                .HasConversion(
                    id    => id.Value.ToString(),
                    value => new TransactionId(Guid.Parse(value)));

            builder.Property(t => t.FromWalletId)
                .HasConversion(w => w.Value, v => new WalletId(v))
                .ToJsonProperty("fromWalletId");

            builder.Property(t => t.ToWalletId)
                .HasConversion(w => w.Value, v => new WalletId(v))
                .ToJsonProperty("toWalletId");

            builder.HasPartitionKey(t => t.FromWalletId);

            builder.OwnsOne(t => t.Amount, vo =>
            {
                vo.Property(a => a.Value).ToJsonProperty("amount");
                vo.Property(a => a.Currency)
                    .HasConversion<string>()
                    .ToJsonProperty("currency");
            });

            builder.Property(t => t.TransactionStatus).HasConversion<string>();
            builder.Property(t => t.SourceType).HasConversion<string>();

            // Motivo de fallo (null cuando la transacción no ha fallado)
            builder.Property(t => t.FailureReason)
                .IsRequired(false)
                .ToJsonProperty("failureReason");
        }
    }
}
