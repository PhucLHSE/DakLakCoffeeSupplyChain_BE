using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ProcessingWasteService : IProcessingWasteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProcessingWasteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IServiceResult> GetAllByUserIdAsync(Guid userId, bool isAdmin)
        {
            // Lấy toàn bộ danh sách người dùng để ánh xạ tên
            var users = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );

            var userMap = users.ToDictionary(u => u.UserId, u => u.Name);

            // Bắt đầu truy vấn Waste
            var query = _unitOfWork.ProcessingWasteRepository.GetAllQueryable()
                .Where(w => !w.IsDeleted);

            if (!isAdmin)
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                {
                    return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy Farmer tương ứng.");
                }

                // Lọc các ProgressId thuộc về farmer
                var progressIds = await _unitOfWork.ProcessingBatchProgressRepository.GetAllQueryable()
                    .Where(p => p.Batch.FarmerId == farmer.FarmerId)
                    .Select(p => p.ProgressId)
                    .ToListAsync();

                query = query.Where(w => progressIds.Contains(w.ProgressId));
            }

            // Thực thi truy vấn
            var wastes = await query.ToListAsync();

            // Map sang DTO
            var dtos = wastes.Select(waste =>
            {
                var recordedByName = waste.RecordedBy.HasValue && userMap.ContainsKey(waste.RecordedBy.Value)
                    ? userMap[waste.RecordedBy.Value]
                    : "N/A";

                return waste.MapToViewAllDto(recordedByName);
            }).ToList();

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dtos);
        }
        public async Task<IServiceResult> GetByIdAsync(Guid wasteId, Guid userId, bool isAdmin)
        {
            // Get all users to resolve RecordedBy names
            var users = await _unitOfWork.UserAccountRepository.GetAllAsync(
                predicate: u => !u.IsDeleted,
                asNoTracking: true
            );
            var userMap = users.ToDictionary(u => u.UserId, u => u.Name);

            // Fetch the waste entry
            var waste = await _unitOfWork.ProcessingWasteRepository.GetByIdAsync(
                predicate: w => w.WasteId == wasteId && !w.IsDeleted,
                include: q => q.Include(w => w.Progress).ThenInclude(p => p.Batch),
                asNoTracking: true
            );

            if (waste == null)
            {
                return new ServiceResult(Const.WARNING_NO_DATA_CODE, "Không tìm thấy dữ liệu chất thải.");
            }

            // If not admin, ensure this waste belongs to the requesting farmer
            if (!isAdmin)
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null || waste.Progress?.Batch?.FarmerId != farmer.FarmerId)
                {
                    return new ServiceResult(Const.FAIL_READ_CODE, "Bạn không có quyền truy cập chất thải này.");
                }
            }

            var recordedByName = waste.RecordedBy.HasValue && userMap.ContainsKey(waste.RecordedBy.Value)
                ? userMap[waste.RecordedBy.Value]
                : "N/A";

            var dto = waste.MapToViewAllDto(recordedByName);

            return new ServiceResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, dto);
        }
    }
}
