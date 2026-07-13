namespace WalletService.Infrastructure.Configuration;

public class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>Cola existente para operaciones de balance (UpdateBalance/SendOperation).</summary>
    public string QueueName { get; set; } = default!;

    /// <summary>Cola donde TransactionService publica TransactionCreated (WalletService consume).</summary>
    public string TransactionCreatedQueueName { get; set; } = "transaction-created";

    /// <summary>Cola donde WalletService publica TransactionCompleted.</summary>
    public string TransactionCompletedQueueName { get; set; } = "transaction-completed";

    /// <summary>Cola donde WalletService publica TransactionFailed.</summary>
    public string TransactionFailedQueueName { get; set; } = "transaction-failed";

    /// <summary>Cola donde TransactionService publica RechargeCreated (WalletService consume).</summary>
    public string RechargeCreatedQueueName { get; set; } = "recharge-created";

    /// <summary>Cola donde WalletService publica RechargeCompleted.</summary>
    public string RechargeCompletedQueueName { get; set; } = "recharge-completed";

    /// <summary>Cola donde WalletService publica RechargeFailed.</summary>
    public string RechargeFailedQueueName { get; set; } = "recharge-failed";
}