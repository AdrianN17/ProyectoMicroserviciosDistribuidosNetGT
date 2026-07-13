using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Transactions.Commands.FailTransaction;

/// <summary>
/// Actualiza el estado de la transacción a FAILED y persiste el motivo.
/// Es idempotente: si ya está FAILED no lanza error.
/// </summary>
public sealed class FailTransactionCommandHandler
    : IRequestHandler<FailTransactionCommand, ErrorOr<Unit>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ILogger<FailTransactionCommandHandler> _logger;

    public FailTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork            unitOfWork,
        ILogger<FailTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork            = unitOfWork;
        _logger                = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(
        FailTransactionCommand request,
        CancellationToken      cancellationToken)
    {
        _logger.LogInformation(
            "Procesando FailTransaction para TransactionId {TransactionId} - Razón: {Reason}",
            request.TransactionId, request.Reason);

        var transaction = await _transactionRepository.GetByIdAsync(
            new TransactionId(request.TransactionId), cancellationToken);

        if (transaction is null)
            return Error.NotFound(
                code:        "Transaction.NotFound",
                description: $"La transacción {request.TransactionId} no fue encontrada.");

        // Idempotencia: si ya está fallida, no hacemos nada
        if (transaction.TransactionStatus == TransactionStatus.FAILED)
        {
            _logger.LogWarning(
                "TransactionId {TransactionId} ya estaba en estado FAILED. Se ignora el evento.",
                request.TransactionId);
            return Unit.Value;
        }

        transaction.Fail(request.Reason);
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "TransactionId {TransactionId} marcada como FAILED. Razón: {Reason}",
            request.TransactionId, request.Reason);

        return Unit.Value;
    }
}

