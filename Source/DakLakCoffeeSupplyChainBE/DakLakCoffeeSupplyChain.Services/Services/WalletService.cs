using DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.Helpers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> Create(WalletCreateDto walletCreateDto, Guid creatorUserId)
        {
            try
            {
                // 1. Kiểm tra người được tạo ví có tồn tại không
                var targetUser = await _unitOfWork.UserAccountRepository.GetByIdAsync(
                    predicate: u => u.UserId == walletCreateDto.UserId && !u.IsDeleted,
                    asNoTracking: true
                );

                if (targetUser == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Người dùng được chỉ định để tạo ví không tồn tại."
                    );
                }

                // 2. Kiểm tra người đó đã có ví cùng loại chưa
                var existingWallets = await _unitOfWork.WalletRepository.GetAllAsync(
                    predicate: w => w.UserId == walletCreateDto.UserId && !w.IsDeleted,
                    asNoTracking: true
                );

                if (existingWallets.Any(w => w.WalletType.Equals(walletCreateDto.WalletType, StringComparison.OrdinalIgnoreCase)))
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Người dùng đã có ví loại '{walletCreateDto.WalletType}'."
                    );
                }

                // 3. Kiểm tra WalletType hợp lệ theo vai trò
                bool isValidRole = false;

                if (walletCreateDto.WalletType.Equals("Business", StringComparison.OrdinalIgnoreCase))
                {
                    // Chỉ tạo nếu là BusinessManager hoặc Staff
                    isValidRole = await _unitOfWork.BusinessManagerRepository.AnyAsync(
                        m => m.UserId == walletCreateDto.UserId && !m.IsDeleted
                    ) || await _unitOfWork.BusinessStaffRepository.AnyAsync(
                        s => s.UserId == walletCreateDto.UserId && !s.IsDeleted
                    );
                }
                else if (walletCreateDto.WalletType.Equals("Farmer", StringComparison.OrdinalIgnoreCase))
                {
                    // Chỉ tạo nếu là Farmer
                    isValidRole = await _unitOfWork.FarmerRepository.AnyAsync(
                        f => f.UserId == walletCreateDto.UserId && !f.IsDeleted
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "WalletType không hợp lệ. Chỉ hỗ trợ 'Business' hoặc 'Farmer'."
                    );
                }

                if (!isValidRole)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Người dùng không phù hợp với loại ví '{walletCreateDto.WalletType}'."
                    );
                }

                // 4. Tạo ví mới
                var newWallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = walletCreateDto.UserId,
                    WalletType = walletCreateDto.WalletType,
                    TotalBalance = walletCreateDto.TotalBalance,
                    LastUpdated = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                };

                await _unitOfWork.WalletRepository.CreateAsync(newWallet);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        newWallet
                    );
                }

                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    Const.FAIL_CREATE_MSG
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.Message
                );
            }
        }

    }
}
