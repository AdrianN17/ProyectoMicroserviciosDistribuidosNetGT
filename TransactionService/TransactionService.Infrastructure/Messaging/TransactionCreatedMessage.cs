using MassTransit;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Contrato del mensaje que TransactionService envía a la cola que WalletService consume.
/// El MessageUrn debe coincidir con el [MessageUrn] declarado en TransactionCreatedMessage
/// del WalletService.Infrastructure.
/// </summary>
[MessageUrn("TransactionService.Application.Events.Integration:TransactionCreated")]
public sealed record TransactionCreatedMessage(
    Guid    TransactionId,
    Guid    FromWalletId,
    Guid    ToWalletId,
    decimal Amount,
    string  Currency,
    string  SourceType
);
