using MediatR;
using ErrorOr;

namespace TransactionService.Application.Transactions.Commands.CreateTransaction;

public sealed record CreateTransactionCommand(
    Guid    TransactionId, // Clave de idempotencia enviada por el cliente
    Guid    FromWalletId,
    Guid    ToWalletId,
    decimal Amount,
    string  Currency,
    string  SourceType
) : IRequest<ErrorOr<Guid>>;