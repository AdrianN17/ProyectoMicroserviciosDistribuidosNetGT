using MediatR;
using ErrorOr;

namespace TransactionService.Application.Transactions.Commands.CreateRecharge;

public sealed record CreateRechargeCommand(
    Guid RechargeId,
    Guid WalletId,
    decimal Amount,
    string Currency,
    string MethodType
) : IRequest<ErrorOr<Guid>>;

