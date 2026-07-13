namespace TransactionService.Infrastructure.Configuration;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    /// <summary>Topic al que TransactionService publica TransactionCreated.</summary>
    public string TransactionCreatedTopic { get; set; } = "transaction-created";

    /// <summary>Topic desde el que TransactionService consume TransactionCompleted.</summary>
    public string TransactionCompletedTopic { get; set; } = "transaction-completed";

    /// <summary>Topic desde el que TransactionService consume TransactionFailed.</summary>
    public string TransactionFailedTopic { get; set; } = "transaction-failed";

    /// <summary>Nombre de la suscripción de este servicio en los topics de entrada.</summary>
    public string SubscriptionName { get; set; } = "transaction-service";

    // Mantenemos QueueName por compatibilidad con recargas y otros flujos.
    public string QueueName { get; set; } = string.Empty;
}

