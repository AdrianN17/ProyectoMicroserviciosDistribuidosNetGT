using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Abstractions.Services;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Application.Transactions.Commands.CreateRecharge;
using DomainRecharge = TransactionService.Domain.Entities.Recharge;

namespace TransactionService.Application.Recharge.Commands.CreateRecharge;

/// <summary>
/// Crea una recarga en estado PENDING y dispara el evento de dominio
/// RechargeCreatedDomainEvent, que a su vez publica en Azure Service Bus.
/// WalletService es el responsable de acreditar el saldo y confirmar.
/// </summary>
public sealed class CreateRechargeCommandHandler : IRequestHandler<CreateRechargeCommand, ErrorOr<Guid>>
{
    private readonly IRechargeRepository  _rechargeRepository;
    private readonly IUnitOfWork          _unitOfWork;
    private readonly IExcnangeReadService _exchangeReadService;
    private readonly ILogger<CreateRechargeCommandHandler> _logger;

    public CreateRechargeCommandHandler(
        IRechargeRepository  rechargeRepository,
        IExcnangeReadService exchangeReadService,
        IUnitOfWork          unitOfWork,
        ILogger<CreateRechargeCommandHandler> logger)
    {
        _rechargeRepository  = rechargeRepository  ?? throw new ArgumentNullException(nameof(rechargeRepository));
        _exchangeReadService  = exchangeReadService ?? throw new ArgumentNullException(nameof(exchangeReadService));
        _unitOfWork           = unitOfWork          ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger               = logger              ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Guid>> Handle(CreateRechargeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creando recarga PENDING para WalletId {WalletId}", request.WalletId);

        // ── Idempotencia: si ya existe la recarga, devolvemos el ID sin duplicar ──
        var existing = await _rechargeRepository.GetByIdAsync(
            new RechargeId(request.RechargeId), cancellationToken);

        if (existing is not null)
        {
            _logger.LogWarning(
                "Recarga {RechargeId} ya existe (idempotencia). Devolviendo ID existente.",
                request.RechargeId);
            return existing.Id.Value;
        }

        if (!EnumParsing.TryParseEnum<CurrencyType>(request.Currency, out var currency))
            return Error.Validation(code: "CurrencyType.Invalid", description: $"CurrencyType '{request.Currency}' no es válido.");

        if (!EnumParsing.TryParseEnum<MethodType>(request.MethodType, out var methodType))
            return Error.Validation(code: "MethodType.Invalid", description: $"MethodType '{request.MethodType}' no es válido.");

        // Obtenemos el tipo de cambio para registrarlo en la recarga (auditoría)
        var exchange = await _exchangeReadService.GetByCurrencyTypeAsync(currency, cancellationToken);
        if (exchange is null)
            return Error.Failure(code: "ExchangeRate.NotFound", description: "El tipo de cambio no está disponible.");

        // Create() inicia en PENDING y levanta RechargeCreatedDomainEvent
        var recharge = DomainRecharge.Create(
            rechargeId:   request.RechargeId,
            walletId:     request.WalletId,
            amount:       request.Amount,
            currency:     currency,
            methodType:   methodType,
            exchangeRate: exchange.value
        );

        await _rechargeRepository.CreateAsync(recharge);

        // SaveChangesAsync dispara RechargeCreatedDomainEvent → publica en ASB
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recarga {RechargeId} creada en estado PENDING.", recharge.Id.Value);

        return recharge.Id.Value;
    }
}


