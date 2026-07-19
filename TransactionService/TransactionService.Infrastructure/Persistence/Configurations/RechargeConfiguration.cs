using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TransactionService.Infrastructure.Persistence.Configurations
{
    public class RechargeConfiguration : IEntityTypeConfiguration<Recharge>
    {
        public void Configure(EntityTypeBuilder<Recharge> builder)
        {
            // Ignorar la colección de domain events — no debe persistirse
            builder.Ignore(r => r.DomainEvents);

            // Clave primaria — RechargeId (record struct) → string
            builder.Property(r => r.Id)
                .HasConversion(
                    id => id.Value.ToString(),
                    value => new RechargeId(Guid.Parse(value)));

            // WalletId es un readonly record struct → Guid plano en JSON
            builder.Property(r => r.WalletId)
                .HasConversion(
                    w => w.Value,
                    v => new WalletId(v))
                .ToJsonProperty("walletId");

            // La partition key apunta directamente a la propiedad ya mapeada
            builder.HasPartitionKey(r => r.WalletId);

            // Amount es clase sellada (tipo referencia) → OwnsOne
            builder.OwnsOne(r => r.Amount, vo =>
            {
                vo.Property(a => a.Value).ToJsonProperty("amount");
                vo.Property(a => a.Currency)
                    .HasConversion<string>()
                    .ToJsonProperty("currency");
            });

            // Enums como string
            builder.Property(r => r.MethodType).HasConversion<string>();
            builder.Property(r => r.RechargeStatus).HasConversion<string>();
        }
    }
}
