using MassTransit;

namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Mensaje que WalletService publica cuando una transferencia falla.
/// El MessageUrn debe coincidir con el declarado en TransactionFailedMessage
/// del TransactionService.Infrastructure.
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:TransactionFailed")]
public sealed record TransactionFailedMessage(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency,
    string Reason
);

