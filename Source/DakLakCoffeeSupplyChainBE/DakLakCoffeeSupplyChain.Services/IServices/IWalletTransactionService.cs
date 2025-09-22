using DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs;
using DakLakCoffeeSupplyChain.Services.Base;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IWalletTransactionService
    {
        // 1. CREATE - Tạo giao dịch
        Task<IServiceResult> CreateAsync(WalletTransactionCreateDto createDto, Guid userId);
        
        // 2. READ - Lấy chi tiết giao dịch
        Task<IServiceResult> GetByIdAsync(Guid transactionId, Guid userId);
        
        // 3. READ - Lấy giao dịch theo ví
        Task<IServiceResult> GetByWalletIdAsync(Guid walletId, Guid userId);
        
        // 4. READ - Lấy giao dịch theo User ID
        Task<IServiceResult> GetTransactionsByUserIdAsync(Guid targetUserId, int pageNumber, int pageSize, Guid currentUserId);
        
        // 5. READ - Lấy giao dịch System Wallet (Admin)
        Task<IServiceResult> GetSystemWalletTransactionsAsync(int pageNumber, int pageSize, Guid currentUserId);
        
        // 5. UPDATE - Cập nhật giao dịch
        Task<IServiceResult> UpdateAsync(Guid transactionId, WalletTransactionUpdateDto updateDto, Guid userId);
        
        // 6. DELETE - Xóa giao dịch (soft delete)
        Task<IServiceResult> DeleteAsync(Guid transactionId, Guid userId);
        
        // 7. HARD DELETE - Xóa vĩnh viễn (chỉ Admin)
        Task<IServiceResult> HardDeleteAsync(Guid transactionId, Guid userId);
        
        // Helper method - Tự động tạo transaction
        Task<IServiceResult> CreateAutoTransactionAsync(Guid walletId, double amount, string transactionType, string? description = null, Guid? paymentId = null);
    }
}
