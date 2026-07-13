using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commmon.Interfaces;

namespace TransactionService.Application.Transactions.Commands.DeleteTransaction;

public sealed class DeleteTransactionCommandHandler
    : IRequestHandler<DeleteTransactionCommand, ErrorOr<Guid>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ILogger<DeleteTransactionCommandHandler> _logger;

    public DeleteTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork            unitOfWork,
        ILogger<DeleteTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _unitOfWork            = unitOfWork            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger                = logger                ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Guid>> Handle(
        DeleteTransactionCommand request,
        CancellationToken        cancellationToken)
    {
        _logger.LogInformation(
            "Cancelando transacción {TransactionId}", request.TransactionId);

        var transactionId = new TransactionId(request.TransactionId);
        var transaction   = await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);

        if (transaction is null)
            return Error.NotFound(
                code:        "Transaction.NotFound",
                description: $"La transacción {request.TransactionId} no fue encontrada.");

        transaction.SoftDelete();

        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Transacción {TransactionId} cancelada exitosamente.", request.TransactionId);

        return transactionId.Value;
    }
}

