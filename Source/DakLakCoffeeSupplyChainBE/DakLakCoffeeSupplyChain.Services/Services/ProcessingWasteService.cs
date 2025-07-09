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


    }
}
