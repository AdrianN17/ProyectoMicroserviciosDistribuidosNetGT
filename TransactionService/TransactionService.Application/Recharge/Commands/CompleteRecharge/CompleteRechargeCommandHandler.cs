using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Recharge.Commands.CompleteRecharge;

/// <summary>
/// Actualiza el estado de la recarga a COMPLETED.
/// Es idempotente: si ya está COMPLETED no lanza error.
/// </summary>
public sealed class CompleteRechargeCommandHandler
    : IRequestHandler<CompleteRechargeCommand, ErrorOr<Unit>>
{
    private readonly IRechargeRepository _rechargeRepository;
    private readonly IUnitOfWork         _unitOfWork;
    private readonly ILogger<CompleteRechargeCommandHandler> _logger;

    public CompleteRechargeCommandHandler(
        IRechargeRepository rechargeRepository,
        IUnitOfWork         unitOfWork,
        ILogger<CompleteRechargeCommandHandler> logger)
    {
        _rechargeRepository = rechargeRepository;
        _unitOfWork         = unitOfWork;
        _logger             = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(
        CompleteRechargeCommand request,
        CancellationToken       cancellationToken)
    {
        _logger.LogInformation(
            "Procesando CompleteRecharge para RechargeId {RechargeId}",
            request.RechargeId);

        var recharge = await _rechargeRepository.GetByIdAsync(
            new RechargeId(request.RechargeId), cancellationToken);

        if (recharge is null)
            return Error.NotFound(
                code:        "Recharge.NotFound",
                description: $"La recarga {request.RechargeId} no fue encontrada.");

        // Idempotencia: si ya está completada, no hacemos nada
        if (recharge.RechargeStatus == RechargeStatus.COMPLETED)
        {
            _logger.LogWarning(
                "RechargeId {RechargeId} ya estaba en estado COMPLETED. Se ignora el evento.",
                request.RechargeId);
            return Unit.Value;
        }

        recharge.Complete();
        await _rechargeRepository.UpdateAsync(recharge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "RechargeId {RechargeId} marcada como COMPLETED exitosamente.",
            request.RechargeId);

        return Unit.Value;
    }
}
