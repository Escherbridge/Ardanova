namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IWalletService
{
    Task<Result<WalletDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WalletDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<WalletDto>> GetByAddressAsync(string address, CancellationToken ct = default);
    Task<Result<WalletDto>> GetPrimaryWalletAsync(string userId, CancellationToken ct = default);
    Task<Result<WalletDto>> CreateAsync(CreateWalletDto dto, CancellationToken ct = default);
    Task<Result<WalletDto>> UpdateAsync(string id, UpdateWalletDto dto, CancellationToken ct = default);
    Task<Result<WalletDto>> VerifyAsync(string id, CancellationToken ct = default);
    Task<Result<WalletDto>> SetPrimaryAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}
