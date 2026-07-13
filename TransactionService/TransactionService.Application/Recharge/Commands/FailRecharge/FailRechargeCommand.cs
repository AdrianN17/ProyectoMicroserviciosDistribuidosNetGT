using ErrorOr;
using MediatR;

namespace TransactionService.Application.Recharge.Commands.FailRecharge;

/// <summary>Comando disparado por el consumer cuando llega RechargeFailed desde ASB.</summary>
public sealed record FailRechargeCommand(Guid RechargeId, string Reason) : IRequest<ErrorOr<Unit>>;
