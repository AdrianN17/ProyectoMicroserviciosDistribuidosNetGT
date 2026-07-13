using MassTransit;

namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Contrato del mensaje TransactionCreated que WalletService consume.
/// El MessageUrn debe coincidir con el tipo publicado por TransactionService.
/// Convención: {Namespace}:{ClassName} del servicio publicador.
/// </summary>
[MessageUrn("TransactionService.Application.Events.Integration:TransactionCreated")]
public sealed record TransactionCreatedMessage(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency,
    string SourceType
);

