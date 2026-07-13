using MediatR;

namespace WalletService.Application.Wallets.Commands.ProcessRechargeCreated;

/// <summary>
/// Comando que WalletService despacha al recibir el evento RechargeCreated desde ASB.
/// Contiene todos los datos necesarios para acreditar el saldo y confirmar o rechazar.
/// </summary>
public sealed record ProcessRechargeCreatedCommand(
    Guid    RechargeId,
    Guid    WalletId,
    decimal Amount,
    string  Currency,
    string  MethodType,
    decimal ExchangeRate
) : IRequest<Unit>;
