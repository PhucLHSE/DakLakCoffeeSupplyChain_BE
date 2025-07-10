using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingWastesDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
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
        private readonly ICodeGenerator _codeGenerator;

        public ProcessingWasteService(IUnitOfWork unitOfWork, ICodeGenerator CodeGenerator )
        {
            _unitOfWork = unitOfWork;
            _codeGenerator = CodeGenerator;
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
        public async Task<IServiceResult> CreateAsync(ProcessingWasteCreateDto dto, Guid userId, bool isAdmin)
        {
            // Nếu không phải admin thì phải kiểm tra quyền truy cập tiến trình
            if (!isAdmin)
            {
                var farmer = await _unitOfWork.FarmerRepository.FindByUserIdAsync(userId);
                if (farmer == null)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không tìm thấy thông tin Farmer.");

                // Kiểm tra tiến trình (progress) có thuộc về farmer không
                var progress = await _unitOfWork.ProcessingBatchProgressRepository.GetByIdAsync(
                    predicate: p => p.ProgressId == dto.ProgressId && !p.IsDeleted,
                    include: q => q.Include(p => p.Batch),
                    asNoTracking: true
                );

                if (progress == null || progress.Batch.FarmerId != farmer.FarmerId)
                    return new ServiceResult(Const.FAIL_CREATE_CODE, "Không có quyền ghi chất thải cho tiến trình này.");
            }

            // Sinh mã chất thải
            var wasteCount = await _unitOfWork.ProcessingWasteRepository.CountByProgressIdAsync(dto.ProgressId);
            var wasteCode = $"WST-{DateTime.Now.Year}-{(wasteCount + 1):D3}";

            // Map DTO sang entity
            var waste = dto.MapToNewEntity(wasteCode, userId);

            // Thêm vào DB
            await _unitOfWork.ProcessingWasteRepository.CreateAsync(waste);
            var saveResult = await _unitOfWork.SaveChangesAsync();

            if (saveResult <= 0)
                return new ServiceResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);

            // Trả về DTO view
            var recordedByName = (await _unitOfWork.UserAccountRepository
                 .GetByIdAsync(u => u.UserId == userId, asNoTracking: true))?.Name ?? "N/A";

            var responseDto = waste.MapToViewAllDto(recordedByName);

            return new ServiceResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG, responseDto);
        }

    }
}
