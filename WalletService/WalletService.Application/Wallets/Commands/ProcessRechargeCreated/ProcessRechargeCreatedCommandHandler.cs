using MediatR;
using Microsoft.Extensions.Logging;
using WalletService.Application.Commmon.Interfaces;
using WalletService.Application.Common.Interfaces;
using WalletService.Application.Contracts;

namespace WalletService.Application.Wallets.Commands.ProcessRechargeCreated;

/// <summary>
/// Handler del comando ProcessRechargeCreated.
/// Aplica el crédito al saldo de la wallet y notifica a TransactionService
/// mediante RechargeCompleted o RechargeFailed.
/// </summary>
public sealed class ProcessRechargeCreatedCommandHandler : IRequestHandler<ProcessRechargeCreatedCommand, Unit>
{
    private readonly IWalletRepository             _walletRepository;
    private readonly IUnitOfWork                   _unitOfWork;
    private readonly IEventPublisher               _eventPublisher;
    private readonly IProcessedRechargeRepository  _processedRechargeRepository;
    private readonly ILogger<ProcessRechargeCreatedCommandHandler> _logger;

    public ProcessRechargeCreatedCommandHandler(
        IWalletRepository            walletRepository,
        IUnitOfWork                  unitOfWork,
        IEventPublisher              eventPublisher,
        IProcessedRechargeRepository processedRechargeRepository,
        ILogger<ProcessRechargeCreatedCommandHandler> logger)
    {
        _walletRepository            = walletRepository            ?? throw new ArgumentNullException(nameof(walletRepository));
        _unitOfWork                  = unitOfWork                  ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher              = eventPublisher              ?? throw new ArgumentNullException(nameof(eventPublisher));
        _processedRechargeRepository = processedRechargeRepository ?? throw new ArgumentNullException(nameof(processedRechargeRepository));
        _logger                      = logger                      ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> Handle(ProcessRechargeCreatedCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando procesamiento de RechargeCreated. RechargeId: {RechargeId}, WalletId: {WalletId}",
            request.RechargeId, request.WalletId);

        // ── Idempotencia ─────────────────────────────────────────────────────────
        if (await _processedRechargeRepository.ExistsAsync(request.RechargeId, cancellationToken))
        {
            _logger.LogWarning(
                "RechargeId {RechargeId} ya fue procesado. Se omite el procesamiento duplicado.",
                request.RechargeId);
            return Unit.Value;
        }

        // ── Validar moneda ────────────────────────────────────────────────────────
        if (!EnumParsing.TryParseEnum<CurrencyType>(request.Currency, out var currency))
        {
            _logger.LogError(
                "Moneda inválida '{Currency}' en RechargeId: {RechargeId}",
                request.Currency, request.RechargeId);

            await _eventPublisher.PublishRechargeFailedAsync(
                new RechargeFailedEvent(
                    request.RechargeId,
                    request.WalletId,
                    request.Amount,
                    request.Currency,
                    "INVALID_CURRENCY"),
                cancellationToken);

            return Unit.Value;
        }

        // ── Buscar Wallet ─────────────────────────────────────────────────────────
        var wallet = await _walletRepository.GetByIdAsync(new WalletId(request.WalletId), cancellationToken);
        if (wallet is null)
        {
            _logger.LogWarning(
                "Wallet no encontrada. RechargeId: {RechargeId}, WalletId: {WalletId}",
                request.RechargeId, request.WalletId);

            await _eventPublisher.PublishRechargeFailedAsync(
                new RechargeFailedEvent(
                    request.RechargeId,
                    request.WalletId,
                    request.Amount,
                    request.Currency,
                    "WALLET_NOT_FOUND"),
                cancellationToken);

            return Unit.Value;
        }

        // ── Validar estado de la wallet ───────────────────────────────────────────
        if (wallet.WalletStatus != WalletStatus.OPERATIVE)
        {
            _logger.LogWarning(
                "Wallet bloqueada. RechargeId: {RechargeId}, WalletId: {WalletId}",
                request.RechargeId, request.WalletId);

            await _eventPublisher.PublishRechargeFailedAsync(
                new RechargeFailedEvent(
                    request.RechargeId,
                    request.WalletId,
                    request.Amount,
                    request.Currency,
                    "WALLET_BLOCKED"),
                cancellationToken);

            return Unit.Value;
        }

        // ── Calcular monto convertido ─────────────────────────────────────────────
        // El ExchangeRate fue fijado en el momento de creación para garantizar auditoría.
        // Si la wallet tiene moneda diferente, aplicamos el tipo de cambio guardado.
        decimal creditAmount;
        if (!EnumParsing.TryParseEnum<CurrencyType>(wallet.WalletBalance.Currency.ToString(), out var walletCurrency))
            walletCurrency = currency;

        if (currency == walletCurrency)
        {
            creditAmount = request.Amount;
        }
        else
        {
            // USD → PEN: amount * exchangeRate  |  PEN → USD: amount / exchangeRate
            creditAmount = currency == CurrencyType.USD
                ? request.Amount * request.ExchangeRate
                : request.Amount / request.ExchangeRate;
        }

        // ── Construir operación de crédito ────────────────────────────────────────
        var creditOperation = new Operation
        {
            Type     = TypeOperation.Addition,
            WalletId = wallet.Id,
            Amount   = creditAmount,
            Currency = walletCurrency
        };

        // ── Persistir de forma atómica ────────────────────────────────────────────
        try
        {
            wallet.WalletBalance.UpdateBalance(creditOperation);
            await _walletRepository.UpdateAsync(wallet, cancellationToken);
            await _processedRechargeRepository.AddAsync(request.RechargeId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Saldo acreditado exitosamente. RechargeId: {RechargeId}, Monto: {Amount} {Currency}",
                request.RechargeId, creditAmount, walletCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error interno al persistir la recarga. RechargeId: {RechargeId}. Se hace rollback.",
                request.RechargeId);

            await _eventPublisher.PublishRechargeFailedAsync(
                new RechargeFailedEvent(
                    request.RechargeId,
                    request.WalletId,
                    request.Amount,
                    request.Currency,
                    "INTERNAL_ERROR"),
                cancellationToken);

            return Unit.Value;
        }

        // ── Publicar RechargeCompleted ─────────────────────────────────────────────
        await _eventPublisher.PublishRechargeCompletedAsync(
            new RechargeCompletedEvent(
                request.RechargeId,
                request.WalletId,
                creditAmount,
                walletCurrency.ToString()),
            cancellationToken);

        return Unit.Value;
    }
}
