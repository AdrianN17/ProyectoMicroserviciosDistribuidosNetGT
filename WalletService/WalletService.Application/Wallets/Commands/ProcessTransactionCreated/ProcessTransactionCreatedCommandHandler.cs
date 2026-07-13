using MediatR;
using Microsoft.Extensions.Logging;
using WalletService.Application.Commmon.Interfaces;
using WalletService.Application.Common.Interfaces;
using WalletService.Application.Contracts;
using WalletService.Domain.Services;

namespace WalletService.Application.Wallets.Commands.ProcessTransactionCreated;

/// <summary>
/// Handler del comando ProcessTransactionCreated.
/// Orquesta el flujo de la Saga Coreografiada para WalletService.
/// </summary>
public sealed class ProcessTransactionCreatedCommandHandler : IRequestHandler<ProcessTransactionCreatedCommand, Unit>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransferDomainService _transferDomainService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IProcessedTransactionRepository _processedTransactionRepository;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly ILogger<ProcessTransactionCreatedCommandHandler> _logger;

    public ProcessTransactionCreatedCommandHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ITransferDomainService transferDomainService,
        IEventPublisher eventPublisher,
        IProcessedTransactionRepository processedTransactionRepository,
        IExchangeRateProvider exchangeRateProvider,
        ILogger<ProcessTransactionCreatedCommandHandler> logger)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _transferDomainService = transferDomainService ?? throw new ArgumentNullException(nameof(transferDomainService));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _processedTransactionRepository = processedTransactionRepository ?? throw new ArgumentNullException(nameof(processedTransactionRepository));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> Handle(ProcessTransactionCreatedCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando procesamiento de TransactionCreated. TransactionId: {TransactionId}",
            request.TransactionId);

        // ──────────────────────────────────────────────────────────────────────────
        // Idempotencia: verificar si ya fue procesado
        // ──────────────────────────────────────────────────────────────────────────
        if (await _processedTransactionRepository.ExistsAsync(request.TransactionId, cancellationToken))
        {
            _logger.LogWarning(
                "Transacción {TransactionId} ya fue procesada. Se omite el procesamiento duplicado.",
                request.TransactionId);
            return Unit.Value;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Paso 1: Buscar Wallet origen
        // ──────────────────────────────────────────────────────────────────────────
        var fromWallet = await _walletRepository.GetByIdAsync(new WalletId(request.FromWalletId), cancellationToken);
        if (fromWallet is null)
        {
            _logger.LogWarning(
                "Wallet origen no encontrada. TransactionId: {TransactionId}, FromWalletId: {FromWalletId}",
                request.TransactionId, request.FromWalletId);

            await _eventPublisher.PublishTransactionFailedAsync(
                new TransactionFailedEvent(
                    request.TransactionId,
                    request.FromWalletId,
                    request.ToWalletId,
                    request.Amount,
                    request.Currency,
                    "FROM_WALLET_NOT_FOUND"),
                cancellationToken);

            return Unit.Value;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Paso 2: Buscar Wallet destino
        // ──────────────────────────────────────────────────────────────────────────
        var toWallet = await _walletRepository.GetByIdAsync(new WalletId(request.ToWalletId), cancellationToken);
        if (toWallet is null)
        {
            _logger.LogWarning(
                "Wallet destino no encontrada. TransactionId: {TransactionId}, ToWalletId: {ToWalletId}",
                request.TransactionId, request.ToWalletId);

            await _eventPublisher.PublishTransactionFailedAsync(
                new TransactionFailedEvent(
                    request.TransactionId,
                    request.FromWalletId,
                    request.ToWalletId,
                    request.Amount,
                    request.Currency,
                    "TO_WALLET_NOT_FOUND"),
                cancellationToken);

            return Unit.Value;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Pasos 3-7: Ejecutar lógica del Domain Service
        //   - Validar estado de wallets (WALLET_BLOCKED)
        //   - Validar límite de transferencia 500 PEN equivalente (LIMIT_EXCEEDED)
        //   - Validar saldo suficiente (INSUFFICIENT_BALANCE)
        //   - Conversión de moneda si aplica
        //   - Debitar/Acreditar saldos en entidades de dominio
        // ──────────────────────────────────────────────────────────────────────────
        if (!EnumParsing.TryParseEnum<CurrencyType>(request.Currency, out var currency))
        {
            _logger.LogError(
                "Moneda inválida '{Currency}' en TransactionId: {TransactionId}",
                request.Currency, request.TransactionId);

            await _eventPublisher.PublishTransactionFailedAsync(
                new TransactionFailedEvent(
                    request.TransactionId,
                    request.FromWalletId,
                    request.ToWalletId,
                    request.Amount,
                    request.Currency,
                    "INTERNAL_ERROR"),
                cancellationToken);

            return Unit.Value;
        }

        var usdToPenRate = _exchangeRateProvider.GetUsdToPenRate();
        var transferResult = _transferDomainService.Execute(
            fromWallet,
            toWallet,
            request.Amount,
            currency,
            usdToPenRate);

        if (!transferResult.IsSuccess)
        {
            var reason = transferResult.FailureReason switch
            {
                TransferFailureReason.WalletBlocked     => "WALLET_BLOCKED",
                TransferFailureReason.LimitExceeded     => "LIMIT_EXCEEDED",
                TransferFailureReason.InsufficientBalance => "INSUFFICIENT_BALANCE",
                _ => "INTERNAL_ERROR"
            };

            _logger.LogWarning(
                "Transferencia rechazada. TransactionId: {TransactionId}, Razón: {Reason}",
                request.TransactionId, reason);

            await _eventPublisher.PublishTransactionFailedAsync(
                new TransactionFailedEvent(
                    request.TransactionId,
                    request.FromWalletId,
                    request.ToWalletId,
                    request.Amount,
                    request.Currency,
                    reason),
                cancellationToken);

            return Unit.Value;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Paso 7: Persistir cambios de forma atómica
        // Si ocurre cualquier excepción: EF Core hace rollback y se publica
        // TransactionFailed con Reason = INTERNAL_ERROR
        // ──────────────────────────────────────────────────────────────────────────
        try
        {
            await _walletRepository.UpdateAsync(fromWallet, cancellationToken);
            await _walletRepository.UpdateAsync(toWallet, cancellationToken);
            await _processedTransactionRepository.AddAsync(request.TransactionId, cancellationToken);

            // SaveChangesAsync es atómico: todo o nada (transacción SQL implícita de EF Core)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transferencia ejecutada exitosamente. TransactionId: {TransactionId}",
                request.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error interno al persistir transferencia. TransactionId: {TransactionId}. Se hace rollback.",
                request.TransactionId);

            await _eventPublisher.PublishTransactionFailedAsync(
                new TransactionFailedEvent(
                    request.TransactionId,
                    request.FromWalletId,
                    request.ToWalletId,
                    request.Amount,
                    request.Currency,
                    "INTERNAL_ERROR"),
                cancellationToken);

            return Unit.Value;
        }

        // ──────────────────────────────────────────────────────────────────────────
        // Paso 8: Publicar TransactionCompleted
        // ──────────────────────────────────────────────────────────────────────────
        await _eventPublisher.PublishTransactionCompletedAsync(
            new TransactionCompletedEvent(
                request.TransactionId,
                request.FromWalletId,
                request.ToWalletId,
                request.Amount,
                request.Currency),
            cancellationToken);

        return Unit.Value;
    }
}

