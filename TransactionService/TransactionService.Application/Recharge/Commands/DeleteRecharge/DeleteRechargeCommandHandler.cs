﻿using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Abstractions.Messaging;
using TransactionService.Application.Abstractions.Services;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Application.Mapper;
using TransactionService.Domain.Common;
using TransactionService.Domain.Interfaces;
using TransactionService.Domain.ValueObjects;

namespace TransactionService.Application.Transactions.Commands.DeleteRecharge;

public sealed class DeleteRechargeCommandHandler : IRequestHandler<DeleteRechargeCommand, ErrorOr<Guid>>
{
    private readonly IRechargeRepository  _rechargeRepository;
    private readonly IUnitOfWork          _unitOfWork;
    private readonly IWalletReadService   _walletReadService;
    private readonly IProducer            _producer;
    private readonly ILogger<DeleteRechargeCommandHandler> _logger;

    public DeleteRechargeCommandHandler(
        IRechargeRepository  rechargeRepository,
        IWalletReadService   walletReadService,
        IUnitOfWork          unitOfWork,
        ILogger<DeleteRechargeCommandHandler> logger,
        IProducer            producer)
    {
        _rechargeRepository = rechargeRepository ?? throw new ArgumentNullException(nameof(rechargeRepository));
        _unitOfWork         = unitOfWork         ?? throw new ArgumentNullException(nameof(unitOfWork));
        _walletReadService  = walletReadService  ?? throw new ArgumentNullException(nameof(walletReadService));
        _producer           = producer           ?? throw new ArgumentNullException(nameof(producer));
        _logger             = logger             ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Guid>> Handle(DeleteRechargeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelando recarga {RechargeId}", request.RechargeId);

        var rechargeId = new RechargeId(request.RechargeId);
        var recharge   = await _rechargeRepository.GetByIdAsync(rechargeId, cancellationToken);

        if (recharge is null)
            return Error.NotFound(
                code:        "Recharge.NotFound",
                description: $"La Recarga '{request.RechargeId}' no existe o ya fue eliminada.");

        // SoftDelete() lanza InvalidDomainStateException si no está en COMPLETED
        try
        {
            recharge.SoftDelete();
        }
        catch (DomainException ex)
        {
            return Error.Conflict(code: ex.Code, description: ex.Message);
        }

        var wallet = await _walletReadService.GetByIdAsync(recharge.WalletId.Value, cancellationToken);
        if (wallet is null)
            return Error.NotFound(
                code:        "Wallet.NotFound",
                description: $"La Wallet '{recharge.WalletId.Value}' no existe o está inactiva.");

        if (!EnumParsing.TryParseEnum<CurrencyType>(wallet.Currency, out var walletCurrency))
            return Error.Validation(
                code:        "CurrencyType.Invalid",
                description: $"CurrencyType of Wallet '{wallet.Currency}' no es válido.");

        await _rechargeRepository.UpdateAsync(recharge, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publicar operación de débito (reversal) a WalletService vía cola existente
        var operation = recharge.ToOperation(walletCurrency);
        await _producer.PublishAsync(operation.ToSendOperation(), cancellationToken);

        _logger.LogInformation("Recarga {RechargeId} cancelada. Reversal enviado a WalletService.", rechargeId.Value);

        return rechargeId.Value;
    }
}

