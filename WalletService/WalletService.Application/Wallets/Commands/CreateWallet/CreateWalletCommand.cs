using MediatR;
using ErrorOr;

namespace WalletService.Application.Wallets.Commands.CreateWallet;

public sealed record CreateWalletCommand(
    Guid WalletId,
    string Name, 
    string LastName, 
    string DocumentType, 
    string DocumentNumber, 
    string Email, 
    string Phone, 
    string Currency, 
    decimal DailyLimit
) : IRequest<ErrorOr<Guid>>;
