namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

public class WalletService : IWalletService
{
    private readonly IRepository<Wallet> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public WalletService(IRepository<Wallet> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<WalletDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var wallet = await _repository.GetByIdAsync(id, ct);
        if (wallet is null)
            return Result<WalletDto>.NotFound($"Wallet with id {id} not found");
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<IReadOnlyList<WalletDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var wallets = await _repository.FindAsync(w => w.UserId == userId, ct);
        return Result<IReadOnlyList<WalletDto>>.Success(_mapper.Map<IReadOnlyList<WalletDto>>(wallets));
    }

    public async Task<Result<WalletDto>> GetByAddressAsync(string address, CancellationToken ct = default)
    {
        var wallet = await _repository.FindOneAsync(w => w.Address == address, ct);
        if (wallet is null)
            return Result<WalletDto>.NotFound($"Wallet with address {address} not found");
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<WalletDto>> GetPrimaryWalletAsync(Guid userId, CancellationToken ct = default)
    {
        var wallet = await _repository.FindOneAsync(w => w.UserId == userId && w.IsPrimary, ct);
        if (wallet is null)
            return Result<WalletDto>.NotFound($"No primary wallet found for user {userId}");
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<WalletDto>> CreateAsync(CreateWalletDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(w => w.Address == dto.Address, ct);
        if (exists)
            return Result<WalletDto>.ValidationError($"Wallet with address {dto.Address} already exists");

        var wallet = Wallet.Create(dto.UserId, dto.Address, dto.Provider, dto.Label, dto.IsPrimary);

        // If this is marked as primary, unset other primary wallets
        if (dto.IsPrimary)
        {
            var existingPrimary = await _repository.FindAsync(w => w.UserId == dto.UserId && w.IsPrimary, ct);
            foreach (var w in existingPrimary)
            {
                w.SetPrimary(false);
                await _repository.UpdateAsync(w, ct);
            }
        }

        await _repository.AddAsync(wallet, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<WalletDto>> UpdateAsync(Guid id, UpdateWalletDto dto, CancellationToken ct = default)
    {
        var wallet = await _repository.GetByIdAsync(id, ct);
        if (wallet is null)
            return Result<WalletDto>.NotFound($"Wallet with id {id} not found");

        wallet.Update(dto.Label, dto.IsPrimary);
        await _repository.UpdateAsync(wallet, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<WalletDto>> VerifyAsync(Guid id, CancellationToken ct = default)
    {
        var wallet = await _repository.GetByIdAsync(id, ct);
        if (wallet is null)
            return Result<WalletDto>.NotFound($"Wallet with id {id} not found");

        wallet.Verify();
        await _repository.UpdateAsync(wallet, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<WalletDto>> SetPrimaryAsync(Guid id, CancellationToken ct = default)
    {
        var wallet = await _repository.GetByIdAsync(id, ct);
        if (wallet is null)
            return Result<WalletDto>.NotFound($"Wallet with id {id} not found");

        // Unset other primary wallets for this user
        var existingPrimary = await _repository.FindAsync(w => w.UserId == wallet.UserId && w.IsPrimary && w.Id != id, ct);
        foreach (var w in existingPrimary)
        {
            w.SetPrimary(false);
            await _repository.UpdateAsync(w, ct);
        }

        wallet.SetPrimary(true);
        await _repository.UpdateAsync(wallet, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<WalletDto>.Success(_mapper.Map<WalletDto>(wallet));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var wallet = await _repository.GetByIdAsync(id, ct);
        if (wallet is null)
            return Result<bool>.NotFound($"Wallet with id {id} not found");

        await _repository.DeleteAsync(wallet, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
