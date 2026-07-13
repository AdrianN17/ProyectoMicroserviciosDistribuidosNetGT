using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Interfaces;

namespace TransactionService.Application.Transactions.Commands.CompleteTransaction;

/// <summary>
/// Actualiza el estado de la transacción a COMPLETED.
/// Es idempotente: si ya está COMPLETED no lanza error.
/// </summary>
public sealed class CompleteTransactionCommandHandler
    : IRequestHandler<CompleteTransactionCommand, ErrorOr<Unit>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ILogger<CompleteTransactionCommandHandler> _logger;

    public CompleteTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork            unitOfWork,
        ILogger<CompleteTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork            = unitOfWork;
        _logger                = logger;
    }

    public async Task<ErrorOr<Unit>> Handle(
        CompleteTransactionCommand request,
        CancellationToken          cancellationToken)
    {
        _logger.LogInformation(
            "Procesando CompleteTransaction para TransactionId {TransactionId}",
            request.TransactionId);

        var transaction = await _transactionRepository.GetByIdAsync(
            new TransactionId(request.TransactionId), cancellationToken);

        if (transaction is null)
            return Error.NotFound(
                code:        "Transaction.NotFound",
                description: $"La transacción {request.TransactionId} no fue encontrada.");

        // Idempotencia: si ya está completada, no hacemos nada
        if (transaction.TransactionStatus == TransactionStatus.COMPLETED)
        {
            _logger.LogWarning(
                "TransactionId {TransactionId} ya estaba en estado COMPLETED. Se ignora el evento.",
                request.TransactionId);
            return Unit.Value;
        }

        transaction.Complete();
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "TransactionId {TransactionId} marcada como COMPLETED exitosamente.",
            request.TransactionId);

        return Unit.Value;
    }
}

