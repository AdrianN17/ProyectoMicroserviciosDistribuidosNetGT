using ErrorOr;
using MediatR;

namespace TransactionService.Application.Transactions.Commands.FailTransaction;

/// <summary>Comando disparado por el consumer cuando llega TransactionFailed desde ASB.</summary>
public sealed record FailTransactionCommand(Guid TransactionId, string Reason) : IRequest<ErrorOr<Unit>>;

