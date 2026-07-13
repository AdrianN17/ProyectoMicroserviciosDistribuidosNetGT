using MediatR;

namespace WalletService.Application.Wallets.Commands.ProcessTransactionCreated;

/// <summary>
/// Comando disparado al recibir el evento TransactionCreated del bus.
/// Representa el inicio del flujo de transferencia en WalletService.
/// </summary>
public sealed record ProcessTransactionCreatedCommand(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency,
    string SourceType
) : IRequest<Unit>;

