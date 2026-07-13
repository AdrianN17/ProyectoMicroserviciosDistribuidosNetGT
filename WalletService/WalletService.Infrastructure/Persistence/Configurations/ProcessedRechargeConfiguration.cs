using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Infrastructure.Persistence.Entities;

namespace WalletService.Infrastructure.Persistence.Configurations;

public sealed class ProcessedRechargeConfiguration : IEntityTypeConfiguration<ProcessedRecharge>
{
    public void Configure(EntityTypeBuilder<ProcessedRecharge> builder)
    {
        builder.ToTable("ProcessedRecharges");

        builder.HasKey(x => x.RechargeId);

        builder.Property(x => x.RechargeId)
            .ValueGeneratedNever();

        builder.Property(x => x.ProcessedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(x => x.RechargeId)
            .IsUnique()
            .HasDatabaseName("IX_ProcessedRecharges_RechargeId");
    }
}
