using MassTransit;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Contrato del mensaje que WalletService publica cuando una transferencia se completa.
/// El MessageUrn debe coincidir con el [MessageUrn] declarado en TransactionCompletedMessage
/// del WalletService.Infrastructure.
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:TransactionCompleted")]
public sealed record TransactionCompletedMessage(
    Guid    TransactionId,
    Guid    FromWalletId,
    Guid    ToWalletId,
    decimal Amount,
    string  Currency
);
