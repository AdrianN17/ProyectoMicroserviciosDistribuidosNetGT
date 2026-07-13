namespace TransactionService.Infrastructure.Configuration;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>Cola donde TransactionService envía TransactionCreated (WalletService consume).</summary>
    public string TransactionCreatedQueueName { get; set; } = "transaction-created";

    /// <summary>Cola donde WalletService publica TransactionCompleted (TransactionService consume).</summary>
    public string TransactionCompletedQueueName { get; set; } = "transaction-completed";

    /// <summary>Cola donde WalletService publica TransactionFailed (TransactionService consume).</summary>
    public string TransactionFailedQueueName { get; set; } = "transaction-failed";

    // Mantenemos QueueName por compatibilidad con recargas y otros flujos.
    public string QueueName { get; set; } = string.Empty;
}

