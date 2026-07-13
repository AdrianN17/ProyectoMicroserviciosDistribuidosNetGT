using ErrorOr;
using MediatR;

namespace TransactionService.Application.Transactions.Commands.CompleteTransaction;

/// <summary>Comando disparado por el consumer cuando llega TransactionCompleted desde ASB.</summary>
public sealed record CompleteTransactionCommand(Guid TransactionId) : IRequest<ErrorOr<Unit>>;

