using ErrorOr;
using MediatR;

namespace TransactionService.Application.Recharge.Commands.CompleteRecharge;

/// <summary>Comando disparado por el consumer cuando llega RechargeCompleted desde ASB.</summary>
public sealed record CompleteRechargeCommand(Guid RechargeId) : IRequest<ErrorOr<Unit>>;
