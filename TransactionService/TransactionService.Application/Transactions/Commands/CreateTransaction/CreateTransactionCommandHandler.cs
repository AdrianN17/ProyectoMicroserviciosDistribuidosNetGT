using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Commmon.Interfaces;

namespace TransactionService.Application.Transactions.Commands.CreateTransaction;

/// <summary>
/// Crea una transacción en estado PENDING y dispara el evento de dominio
/// TransactionCreatedDomainEvent, que a su vez publica en Azure Service Bus.
/// NO llama a WalletService ni valida saldo/límites.
/// </summary>
public sealed class CreateTransactionCommandHandler
    : IRequestHandler<CreateTransactionCommand, ErrorOr<Guid>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork            unitOfWork,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _unitOfWork            = unitOfWork            ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger                = logger                ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Guid>> Handle(
        CreateTransactionCommand request,
        CancellationToken        cancellationToken)
    {
        _logger.LogInformation(
            "Creando transacción PENDING de {FromWalletId} → {ToWalletId}",
            request.FromWalletId, request.ToWalletId);

        if (!EnumParsing.TryParseEnum<CurrencyType>(request.Currency, out var currency))
            return Error.Validation(
                code:        "CurrencyType.Invalid",
                description: $"CurrencyType '{request.Currency}' no es válido.");

        if (!EnumParsing.TryParseEnum<SourceType>(request.SourceType, out var sourceType))
            return Error.Validation(
                code:        "SourceType.Invalid",
                description: $"SourceType '{request.SourceType}' no es válido.");

        // Create() establece estado PENDING y agrega TransactionCreatedDomainEvent
        var transaction = Transaction.Create(
            fromWalletId: request.FromWalletId,
            toWalletId:   request.ToWalletId,
            amount:       request.Amount,
            currency:     currency,
            sourceType:   sourceType
        );

        await _transactionRepository.CreateAsync(transaction);

        // SaveChangesAsync del DbContext dispara los domain events mediante MediatR,
        // lo que a su vez publica TransactionCreatedIntegrationEvent en ASB.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Transacción {TransactionId} creada en estado PENDING.",
            transaction.Id.Value);

        return transaction.Id.Value;
    }
}