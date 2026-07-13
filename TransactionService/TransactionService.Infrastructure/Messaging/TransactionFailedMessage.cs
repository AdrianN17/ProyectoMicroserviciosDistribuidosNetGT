using MassTransit;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Contrato del mensaje que WalletService publica cuando una transferencia falla.
/// El MessageUrn debe coincidir con el [MessageUrn] declarado en TransactionFailedMessage
/// del WalletService.Infrastructure.
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:TransactionFailed")]
public sealed record TransactionFailedMessage(
    Guid    TransactionId,
    Guid    FromWalletId,
    Guid    ToWalletId,
    decimal Amount,
    string  Currency,
    string  Reason
);
