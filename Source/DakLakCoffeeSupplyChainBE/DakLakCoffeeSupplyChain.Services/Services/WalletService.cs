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
                var targetUser = await _unitOfWork.UserAccountRepository.GetByIdAsync(walletCreateDto.UserId);
                if (targetUser == null || targetUser.IsDeleted)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Người dùng được chỉ định để tạo ví không tồn tại."
                    );
                }

                // 2. Kiểm tra người đó đã có ví cùng loại chưa
                var existingWallets = await _unitOfWork.WalletRepository.GetAllAsync();
                var userWallets = existingWallets.Where(w => w.UserId == walletCreateDto.UserId && !w.IsDeleted);

                if (userWallets.Any(w => w.WalletType.Equals(walletCreateDto.WalletType, StringComparison.OrdinalIgnoreCase)))
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Người dùng đã có ví loại '{walletCreateDto.WalletType}'."
                    );
                }

                // 3. Tạo ví mới
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

        public async Task<IServiceResult> GetAllAsync(Guid userId)
        {
            try
            {
                // Lấy tất cả ví
                var allWallets = await _unitOfWork.WalletRepository.GetAllAsync();
                var wallets = allWallets.Where(w => !w.IsDeleted).ToList();

                var walletDtos = wallets.Select(w => new WalletListDto
                {
                    WalletId = w.WalletId,
                    UserId = w.UserId ?? Guid.Empty,
                    WalletType = w.WalletType,
                    TotalBalance = w.TotalBalance,
                    LastUpdated = w.LastUpdated,
                    UserName = null,
                    UserCode = null
                }).ToList();

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách ví thành công", walletDtos);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> GetByIdAsync(Guid walletId, Guid userId)
        {
            try
            {
                // Lấy ví theo ID
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy ví.");
                }

                var walletDto = new WalletDetailDto
                {
                    WalletId = wallet.WalletId,
                    UserId = wallet.UserId ?? Guid.Empty,
                    WalletType = wallet.WalletType,
                    TotalBalance = wallet.TotalBalance,
                    LastUpdated = wallet.LastUpdated,
                    IsDeleted = wallet.IsDeleted,
                    UserName = null,
                    UserCode = null,
                    TotalTransactions = 0,
                    TotalInflow = 0,
                    TotalOutflow = 0
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy chi tiết ví thành công", walletDto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> UpdateAsync(Guid walletId, WalletUpdateDto dto, Guid userId)
        {
            try
            {
                // Lấy ví để cập nhật
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy ví.");
                }

                // Cập nhật thông tin
                if (!string.IsNullOrEmpty(dto.WalletType))
                {
                    wallet.WalletType = dto.WalletType;
                }

                if (dto.TotalBalance.HasValue)
                {
                    if (dto.TotalBalance.Value < 0)
                    {
                        return new ServiceResult(Const.ERROR_VALIDATION_CODE, "Số dư không thể âm.");
                    }
                    wallet.TotalBalance = dto.TotalBalance.Value;
                }

                wallet.LastUpdated = DateHelper.NowVietnamTime();

                await _unitOfWork.WalletRepository.UpdateAsync(wallet);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật ví thành công", wallet);
                }

                return new ServiceResult(Const.FAIL_UPDATE_CODE, "Cập nhật ví thất bại");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> DeleteAsync(Guid walletId, Guid userId)
        {
            try
            {
                // Lấy ví để xóa
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy ví.");
                }

                // Soft delete
                wallet.IsDeleted = true;
                wallet.LastUpdated = DateHelper.NowVietnamTime();

                await _unitOfWork.WalletRepository.UpdateAsync(wallet);
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa ví thành công");
                }

                return new ServiceResult(Const.FAIL_DELETE_CODE, "Xóa ví thất bại");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> GetMyWalletAsync(Guid userId)
        {
            try
            {
                var allWallets = await _unitOfWork.WalletRepository.GetAllAsync();
                var wallet = allWallets.FirstOrDefault(w => w.UserId == userId && !w.IsDeleted);

                if (wallet == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Bạn chưa có ví.");
                }

                var walletDto = new WalletDetailDto
                {
                    WalletId = wallet.WalletId,
                    UserId = wallet.UserId ?? Guid.Empty,
                    WalletType = wallet.WalletType,
                    TotalBalance = wallet.TotalBalance,
                    LastUpdated = wallet.LastUpdated,
                    IsDeleted = wallet.IsDeleted,
                    UserName = null,
                    UserCode = null,
                    TotalTransactions = 0,
                    TotalInflow = 0,
                    TotalOutflow = 0
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy thông tin ví thành công", walletDto);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> GetWalletBalanceAsync(Guid walletId, Guid userId)
        {
            try
            {
                // Lấy số dư ví
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy ví.");
                }

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy số dư ví thành công", new { 
                    WalletId = wallet.WalletId, 
                    TotalBalance = wallet.TotalBalance,
                    LastUpdated = wallet.LastUpdated 
                });
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> CreateTopupPaymentAsync(WalletTopupRequestDto request, Guid userId)
        {
            try
            {
                // Kiểm tra ví có tồn tại và thuộc về user không
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(request.WalletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy ví.");
                }

                if (wallet.UserId != userId)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền nạp tiền vào ví này.");
                }

                // Tạo transaction ID
                var transactionId = Guid.NewGuid().ToString("N");
                var now = DateHelper.NowVietnamTime();

                // Tạo payment record
                var cfg = (await _unitOfWork.PaymentConfigurationRepository.GetAllAsync()).FirstOrDefault();
                if (cfg == null)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Hệ thống thanh toán chưa được cấu hình.");
                }

                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Email = string.Empty,
                    ConfigId = cfg.ConfigId,
                    UserId = userId,
                    PaymentCode = transactionId,
                    PaymentAmount = (int)request.Amount,
                    PaymentMethod = "VNPay",
                    PaymentPurpose = "WalletTopup",
                    PaymentStatus = "Pending",
                    PaymentTime = null,
                    AdminVerified = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    RelatedEntityId = request.WalletId,
                    IsDeleted = false
                };

                await _unitOfWork.PaymentRepository.CreateAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                var response = new WalletTopupResponseDto
                {
                    TransactionId = transactionId,
                    Amount = request.Amount,
                    Message = "Tạo giao dịch nạp tiền thành công"
                };

                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo giao dịch nạp tiền thành công", response);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> ProcessTopupPaymentAsync(string transactionId, double amount, Guid userId)
        {
            try
            {
                // Tìm payment record - cắt 20 ký tự đầu để match với PaymentCode
                var paymentCode = transactionId.Length > 20 ? transactionId[..20] : transactionId;
                var payment = (await _unitOfWork.PaymentRepository.GetAllAsync(p => p.PaymentCode == paymentCode)).FirstOrDefault();
                if (payment == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy giao dịch.");
                }

                if (payment.UserId != userId)
                {
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Bạn không có quyền xử lý giao dịch này.");
                }

                if (payment.PaymentStatus == "Success")
                {
                    return new ServiceResult(Const.SUCCESS_READ_CODE, "Giao dịch đã được xử lý thành công.");
                }

                // Cập nhật payment status
                var now = DateHelper.NowVietnamTime();
                payment.PaymentStatus = "Success";
                payment.PaymentTime = now;
                payment.UpdatedAt = now;
                payment.AdminVerified = true;

                await _unitOfWork.PaymentRepository.UpdateAsync(payment);

                // Cập nhật số dư ví
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(payment.RelatedEntityId ?? Guid.Empty);
                if (wallet != null && !wallet.IsDeleted)
                {
                    wallet.TotalBalance += amount;
                    wallet.LastUpdated = now;
                    await _unitOfWork.WalletRepository.UpdateAsync(wallet);

                    // Tạo wallet transaction record
                    var walletTransaction = new WalletTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        WalletId = wallet.WalletId,
                        PaymentId = payment.PaymentId,
                        Amount = amount,
                        TransactionType = "Topup",
                        Description = "Nạp tiền vào ví qua VNPay",
                        CreatedAt = now,
                        IsDeleted = false
                    };

                    await _unitOfWork.WalletTransactionRepository.CreateAsync(walletTransaction);
                }

                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Nạp tiền vào ví thành công", new { 
                    Amount = amount,
                    NewBalance = wallet?.TotalBalance ?? 0
                });
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }

        public async Task<IServiceResult> DirectTopupAsync(Guid userId, double amount, string? description)
        {
            try
            {
                // Lấy ví của user
                var allWallets = await _unitOfWork.WalletRepository.GetAllAsync();
                var wallet = allWallets.FirstOrDefault(w => w.UserId == userId && !w.IsDeleted);

                if (wallet == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy ví của người dùng.");
                }

                // Sử dụng WalletTransactionService để tự động tạo transaction
                var walletTransactionService = new WalletTransactionService(_unitOfWork);
                var transactionResult = await walletTransactionService.CreateAutoTransactionAsync(
                    wallet.WalletId, 
                    amount, 
                    "DirectTopup", 
                    description ?? "Nạp tiền trực tiếp (Test)"
                );

                if (transactionResult.Status != Const.SUCCESS_CREATE_CODE)
                {
                    return transactionResult;
                }

                // Lấy lại ví để có số dư mới nhất
                var updatedWallet = await _unitOfWork.WalletRepository.GetByIdAsync(wallet.WalletId);

                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Nạp tiền trực tiếp thành công", new { 
                    Amount = amount,
                    NewBalance = updatedWallet?.TotalBalance ?? wallet.TotalBalance,
                    Transaction = transactionResult.Data
                });
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.ERROR_EXCEPTION, ex.Message);
            }
        }
    }
}
