namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IWalletService
{
    Task<Result<WalletDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WalletDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<WalletDto>> GetByAddressAsync(string address, CancellationToken ct = default);
    Task<Result<WalletDto>> GetPrimaryWalletAsync(Guid userId, CancellationToken ct = default);
    Task<Result<WalletDto>> CreateAsync(CreateWalletDto dto, CancellationToken ct = default);
    Task<Result<WalletDto>> UpdateAsync(Guid id, UpdateWalletDto dto, CancellationToken ct = default);
    Task<Result<WalletDto>> VerifyAsync(Guid id, CancellationToken ct = default);
    Task<Result<WalletDto>> SetPrimaryAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
