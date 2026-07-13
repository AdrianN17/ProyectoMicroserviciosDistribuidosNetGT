using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Recharge.Commands.FailRecharge;

/// <summary>
/// Actualiza el estado de la recarga a FAILED y persiste el motivo.
/// Es idempotente: si ya está FAILED no lanza error.
/// </summary>
public sealed class FailRechargeCommandHandler
    : IRequestHandler<FailRechargeCommand, ErrorOr<Unit>>
{
    private readonly IRechargeRepository _rechargeRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ILogger<FailRechargeCommandHandler> _logger;

    public FailRechargeCommandHandler(
        IRechargeRepository rechargeRepository,
        IUnitOfWork         unitOfWork,
        ILogger<FailRechargeCommandHandler> logger)
    {
        _rechargeRepository = rechargeRepository;
        _unitOfWork         = unitOfWork;
        _logger             = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(
        FailRechargeCommand request,
        CancellationToken   cancellationToken)
    {
        _logger.LogInformation(
            "Procesando FailRecharge para RechargeId {RechargeId} - Razón: {Reason}",
            request.RechargeId, request.Reason);

        var recharge = await _rechargeRepository.GetByIdAsync(
            new RechargeId(request.RechargeId), cancellationToken);

        if (recharge is null)
            return Error.NotFound(
                code:        "Recharge.NotFound",
                description: $"La recarga {request.RechargeId} no fue encontrada.");

        // Idempotencia: si ya está fallida, no hacemos nada
        if (recharge.RechargeStatus == RechargeStatus.FAILED)
        {
            _logger.LogWarning(
                "RechargeId {RechargeId} ya estaba en estado FAILED. Se ignora el evento.",
                request.RechargeId);
            return Unit.Value;
        }

        recharge.Fail(request.Reason);
        await _rechargeRepository.UpdateAsync(recharge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "RechargeId {RechargeId} marcada como FAILED. Razón: {Reason}",
            request.RechargeId, request.Reason);

        return Unit.Value;
    }
}
