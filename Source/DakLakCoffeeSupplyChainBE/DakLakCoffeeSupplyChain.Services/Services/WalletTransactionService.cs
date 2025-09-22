using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs;
using DakLakCoffeeSupplyChain.Repositories.IRepositories;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class WalletTransactionService : IWalletTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public WalletTransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> CreateAsync(WalletTransactionCreateDto createDto, Guid userId)
        {
            try
            {
                // 1. Kiểm tra ví có tồn tại không
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(createDto.WalletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Ví không tồn tại.");
                }

                // 2. Kiểm tra quyền truy cập ví
                if (wallet.UserId != userId)
                {
                    // Kiểm tra nếu user là BusinessManager, BusinessStaff hoặc Admin
                    var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                    if (user == null || (!user.Role.RoleName.Equals("BusinessManager", StringComparison.OrdinalIgnoreCase) && 
                                        !user.Role.RoleName.Equals("BusinessStaff", StringComparison.OrdinalIgnoreCase) &&
                                        !user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập ví này.");
                    }
                }

                // 3. Tạo giao dịch mới
                var transaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = createDto.WalletId,
                    PaymentId = createDto.PaymentId,
                    Amount = createDto.Amount,
                    TransactionType = createDto.TransactionType,
                    Description = createDto.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.WalletTransactionRepository.CreateAsync(transaction);

                // 4. Cập nhật số dư ví
                if (createDto.TransactionType == "TopUp")
                {
                    wallet.TotalBalance += createDto.Amount;
                }
                else
                {
                    // Kiểm tra số dư có đủ không
                    if (wallet.TotalBalance < createDto.Amount)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Số dư ví không đủ để thực hiện giao dịch.");
                    }
                    wallet.TotalBalance -= createDto.Amount;
                }

                wallet.LastUpdated = DateTime.UtcNow;
                await _unitOfWork.WalletRepository.UpdateAsync(wallet);

                await _unitOfWork.SaveChangesAsync();

                // 5. Trả về kết quả
                var transactionDetail = await GetTransactionDetailAsync(transaction.TransactionId);
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo giao dịch thành công.", transactionDetail);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, $"Lỗi khi tạo giao dịch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GetByIdAsync(Guid transactionId, Guid userId)
        {
            try
            {
                var transaction = await _unitOfWork.WalletTransactionRepository
                    .GetAllQueryable()
                    .Include(t => t.Wallet)
                    .ThenInclude(w => w.User)
                    .Include(t => t.Payment)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId && !t.IsDeleted);

                if (transaction == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Giao dịch không tồn tại.");
                }

                // Kiểm tra quyền truy cập
                if (transaction.Wallet.UserId != userId)
                {
                    var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                    if (user == null || (!user.Role.RoleName.Equals("BusinessManager", StringComparison.OrdinalIgnoreCase) && 
                                        !user.Role.RoleName.Equals("BusinessStaff", StringComparison.OrdinalIgnoreCase) &&
                                        !user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập giao dịch này.");
                    }
                }

                var transactionDetail = MapToDetailDto(transaction);
                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy thông tin giao dịch thành công.", transactionDetail);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_READ_CODE, $"Lỗi khi lấy thông tin giao dịch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> GetByWalletIdAsync(Guid walletId, Guid userId)
        {
            try
            {
                // Kiểm tra quyền truy cập ví
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Ví không tồn tại.");
                }

                if (wallet.UserId != userId)
                {
                    var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                    if (user == null || (!user.Role.RoleName.Equals("BusinessManager", StringComparison.OrdinalIgnoreCase) && 
                                        !user.Role.RoleName.Equals("BusinessStaff", StringComparison.OrdinalIgnoreCase) &&
                                        !user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập ví này.");
                    }
                }

                var transactions = await _unitOfWork.WalletTransactionRepository.GetByWalletIdAsync(walletId);
                var transactionList = transactions.Select(MapToListDto).ToList();

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy danh sách giao dịch thành công.", transactionList);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_READ_CODE, $"Lỗi khi lấy danh sách giao dịch: {ex.Message}");
            }
        }


        public async Task<IServiceResult> UpdateAsync(Guid transactionId, WalletTransactionUpdateDto updateDto, Guid userId)
        {
            try
            {
                var transaction = await _unitOfWork.WalletTransactionRepository.GetByIdAsync(transactionId);
                if (transaction == null || transaction.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Giao dịch không tồn tại.");
                }

                // Kiểm tra quyền truy cập
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(transaction.WalletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Ví không tồn tại.");
                }

                if (wallet.UserId != userId)
                {
                    var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                    if (user == null || (!user.Role.RoleName.Equals("BusinessManager", StringComparison.OrdinalIgnoreCase) && 
                                        !user.Role.RoleName.Equals("BusinessStaff", StringComparison.OrdinalIgnoreCase) &&
                                        !user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền cập nhật giao dịch này.");
                    }
                }

                // Chỉ cho phép cập nhật description
                transaction.Description = updateDto.Description;
                await _unitOfWork.WalletTransactionRepository.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                var updatedTransaction = await GetTransactionDetailAsync(transactionId);
                return new ServiceResult(Const.SUCCESS_UPDATE_CODE, "Cập nhật giao dịch thành công.", updatedTransaction);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_UPDATE_CODE, $"Lỗi khi cập nhật giao dịch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> DeleteAsync(Guid transactionId, Guid userId)
        {
            try
            {
                var transaction = await _unitOfWork.WalletTransactionRepository.GetByIdAsync(transactionId);
                if (transaction == null || transaction.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Giao dịch không tồn tại.");
                }

                // Kiểm tra quyền truy cập
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(transaction.WalletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Ví không tồn tại.");
                }

                if (wallet.UserId != userId)
                {
                    var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                    if (user == null || (!user.Role.RoleName.Equals("BusinessManager", StringComparison.OrdinalIgnoreCase) && 
                                        !user.Role.RoleName.Equals("BusinessStaff", StringComparison.OrdinalIgnoreCase) &&
                                        !user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                    {
                        return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền xóa giao dịch này.");
                    }
                }

                // Soft delete
                transaction.IsDeleted = true;
                await _unitOfWork.WalletTransactionRepository.UpdateAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa giao dịch thành công.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_DELETE_CODE, $"Lỗi khi xóa giao dịch: {ex.Message}");
            }
        }

        public async Task<IServiceResult> HardDeleteAsync(Guid transactionId, Guid userId)
        {
            try
            {
                // Chỉ Admin mới có quyền hard delete
                var user = await _unitOfWork.UserAccountRepository.GetByIdAsync(userId);
                if (user == null || !user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return new ServiceResult(Const.FAIL_DELETE_CODE, "Chỉ Admin mới có quyền xóa vĩnh viễn giao dịch.");
                }

                var transaction = await _unitOfWork.WalletTransactionRepository.GetByIdAsync(transactionId);

                if (transaction == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Giao dịch không tồn tại.");
                }

                // Hard delete - xóa vĩnh viễn khỏi database
                await _unitOfWork.WalletTransactionRepository.RemoveAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(Const.SUCCESS_DELETE_CODE, "Xóa vĩnh viễn giao dịch thành công.");
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_DELETE_CODE, $"Lỗi khi xóa vĩnh viễn giao dịch: {ex.Message}");
            }
        }


        private async Task<WalletTransactionDetailDto> GetTransactionDetailAsync(Guid transactionId)
        {
            var transaction = await _unitOfWork.WalletTransactionRepository
                .GetAllQueryable()
                .Include(t => t.Wallet)
                .ThenInclude(w => w.User)
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            return MapToDetailDto(transaction);
        }

        private WalletTransactionDetailDto MapToDetailDto(WalletTransaction transaction)
        {
            return new WalletTransactionDetailDto
            {
                TransactionId = transaction.TransactionId,
                WalletId = transaction.WalletId,
                PaymentId = transaction.PaymentId,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                IsDeleted = transaction.IsDeleted,
                WalletType = transaction.Wallet?.WalletType,
                UserName = transaction.Wallet?.User?.Name,
                UserCode = transaction.Wallet?.User?.UserCode,
                PaymentStatus = transaction.Payment?.PaymentStatus
            };
        }

        private WalletTransactionListDto MapToListDto(WalletTransaction transaction)
        {
            return new WalletTransactionListDto
            {
                TransactionId = transaction.TransactionId,
                WalletId = transaction.WalletId,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                IsDeleted = transaction.IsDeleted,
                WalletType = transaction.Wallet?.WalletType,
                UserName = transaction.Wallet?.User?.Name
            };
        }

        /// <summary>
        /// Tự động tạo transaction khi có thao tác với ví (nạp/rút tiền)
        /// </summary>
        public async Task<IServiceResult> CreateAutoTransactionAsync(Guid walletId, double amount, string transactionType, string? description = null, Guid? paymentId = null)
        {
            try
            {
                // 1. Kiểm tra ví có tồn tại không
                var wallet = await _unitOfWork.WalletRepository.GetByIdAsync(walletId);
                if (wallet == null || wallet.IsDeleted)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Ví không tồn tại.");
                }

                // 2. Tạo giao dịch mới
                var transaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = walletId,
                    PaymentId = paymentId,
                    Amount = amount,
                    TransactionType = transactionType,
                    Description = description ?? $"Giao dịch {transactionType}",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _unitOfWork.WalletTransactionRepository.CreateAsync(transaction);

                // 3. Cập nhật số dư ví
                if (transactionType == "TopUp" || transactionType == "DirectTopup")
                {
                    wallet.TotalBalance += amount;
                }
                else
                {
                    // Kiểm tra số dư có đủ không
                    if (wallet.TotalBalance < amount)
                    {
                        return new ServiceResult(Const.FAIL_CREATE_CODE, "Số dư ví không đủ để thực hiện giao dịch.");
                    }
                    wallet.TotalBalance -= amount;
                }

                wallet.LastUpdated = DateTime.UtcNow;
                await _unitOfWork.WalletRepository.UpdateAsync(wallet);

                await _unitOfWork.SaveChangesAsync();

                // 4. Trả về kết quả
                var transactionDetail = await GetTransactionDetailAsync(transaction.TransactionId);
                return new ServiceResult(Const.SUCCESS_CREATE_CODE, "Tạo giao dịch tự động thành công.", transactionDetail);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_CREATE_CODE, $"Lỗi khi tạo giao dịch tự động: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy giao dịch theo User ID (đơn giản hóa)
        /// </summary>
        public async Task<IServiceResult> GetTransactionsByUserIdAsync(Guid targetUserId, int pageNumber, int pageSize, Guid currentUserId)
        {
            try
            {
                // Kiểm tra quyền truy cập: nếu không phải Admin/BM/BS thì chỉ được xem giao dịch của chính mình
                var currentUser = await _unitOfWork.UserAccountRepository.GetByIdAsync(currentUserId);
                if (currentUser == null)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Không tìm thấy thông tin người dùng.");
                }

                // Nếu không phải Admin/BM/BS và không phải xem giao dịch của chính mình
                if (!currentUser.Role.RoleName.Equals("BusinessManager", StringComparison.OrdinalIgnoreCase) && 
                    !currentUser.Role.RoleName.Equals("BusinessStaff", StringComparison.OrdinalIgnoreCase) &&
                    !currentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
                    targetUserId != currentUserId)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn chỉ có thể xem giao dịch của chính mình.");
                }

                // Lấy ví của target user
                var allWallets = await _unitOfWork.WalletRepository.GetAllAsync();
                var targetUserWallets = allWallets.Where(w => w.UserId == targetUserId && !w.IsDeleted).ToList();

                if (!targetUserWallets.Any())
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Người dùng này chưa có ví nào.");
                }

                // Lấy tất cả giao dịch của target user từ các ví
                var allTransactions = new List<WalletTransaction>();
                foreach (var wallet in targetUserWallets)
                {
                    var walletTransactions = await _unitOfWork.WalletTransactionRepository.GetByWalletIdAsync(wallet.WalletId);
                    allTransactions.AddRange(walletTransactions);
                }

                // Sắp xếp theo thời gian tạo (mới nhất trước)
                allTransactions = allTransactions
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();

                // Phân trang
                var totalCount = allTransactions.Count;
                var pagedTransactions = allTransactions
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var transactionList = pagedTransactions.Select(MapToListDto).ToList();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var result = new WalletTransactionSearchResultDto
                {
                    Data = transactionList,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                return new ServiceResult(Const.SUCCESS_READ_CODE, "Lấy giao dịch của người dùng thành công.", result);
            }
            catch (Exception ex)
            {
                return new ServiceResult(Const.FAIL_READ_CODE, $"Lỗi khi lấy giao dịch của người dùng: {ex.Message}");
            }
        }
    }
}
