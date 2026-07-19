namespace WalletService.Application.Wallets.Commands.CreateWallet;

using WalletService.Application.Commmon.Interfaces;
using WalletService.Application.Common.Helpers;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletService.Domain.Enums;

public sealed class CreateWalletCommandHandler : IRequestHandler<CreateWalletCommand, ErrorOr<Guid>>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateWalletCommandHandler> _logger;
    
    public CreateWalletCommandHandler(IWalletRepository walletRepository, IUnitOfWork unitOfWork, ILogger<CreateWalletCommandHandler> logger)
    {
        _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<ErrorOr<Guid>> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating wallet with document number {DocumentNumber}", request.DocumentNumber);

        // Idempotencia: si ya existe la wallet con ese ID, la devolvemos sin duplicar
        var existing = await _walletRepository.GetByIdAsync(new WalletId(request.WalletId), cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning("Wallet {WalletId} ya existe (idempotencia). Devolviendo ID existente.", request.WalletId);
            return existing.Id.Value;
        }

        if (!EnumParsing.TryParseEnum<DocumentType>(request.DocumentType, out var documentType))
            return Error.Validation(code: "DocumentType.Invalid", description: $"DocumentType '{request.DocumentType}' no es válido.");
        
        if (!EnumParsing.TryParseEnum<CurrencyType>(request.Currency, out var currency))
            return Error.Validation(code: "CurrencyType.Invalid", description: $"CurrencyType '{request.Currency}' no es válido.");
        
        var wallet = Wallet.Create(
            request.WalletId,
            request.Name, 
            request.LastName, 
            documentType, 
            request.DocumentNumber, 
            request.Email, 
            request.Phone, 
            currency, 
            request.DailyLimit
        );
        
        await _walletRepository.CreateAsync(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return wallet.Id.Value;
    }
}