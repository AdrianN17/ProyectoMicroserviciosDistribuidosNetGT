using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletService.Infrastructure.Persistence.Entities;

namespace WalletService.Infrastructure.Persistence.Configurations;

public sealed class ProcessedTransactionConfiguration : IEntityTypeConfiguration<ProcessedTransaction>
{
    public void Configure(EntityTypeBuilder<ProcessedTransaction> builder)
    {
        builder.ToTable("ProcessedTransactions");

        builder.HasKey(x => x.TransactionId);

        builder.Property(x => x.TransactionId)
            .ValueGeneratedNever();

        builder.Property(x => x.ProcessedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        // Índice único para evitar inserciones duplicadas concurrentes
        builder.HasIndex(x => x.TransactionId)
            .IsUnique()
            .HasDatabaseName("IX_ProcessedTransactions_TransactionId");
    }
}

