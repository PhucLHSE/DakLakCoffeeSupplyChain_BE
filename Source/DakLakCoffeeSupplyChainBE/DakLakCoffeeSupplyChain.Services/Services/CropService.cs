using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Services.Generators;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CropService : ICropService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public CropService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            // Get farmer from userId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == userId &&
                   !f.IsDeleted,
                asNoTracking: true
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get crops from repository
            var crops = await _unitOfWork.CropRepository.GetAllAsync(
                predicate: c => 
                   c.IsDeleted == false && 
                   c.CreatedBy == farmer.FarmerId,
                orderBy: query => query.OrderBy(c => c.CreatedAt),
                asNoTracking: true
            );

            // Check if no data
            if (crops == null ||
                !crops.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CropViewAllDto>()  // Return empty list
                );
            }
            else
            {
                // Convert to DTO list for client
                var cropDtos = crops
                    .Select(crops => crops.MapToCropViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cropDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid cropId, Guid userId)
        {
            // Get farmer from userId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == userId &&
                   !f.IsDeleted,
                asNoTracking: true
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get crop by ID and check ownership
            var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   c.IsDeleted == false &&
                   c.CreatedBy == farmer.FarmerId,
                asNoTracking: true
            );

            if (crop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền truy cập."
                );
            }

            var cropDto = crop.MapToCropViewDetailsDto();

            return new ServiceResult(
                Const.SUCCESS_READ_CODE,
                Const.SUCCESS_READ_MSG,
                cropDto
            );
        }

        public async Task<IServiceResult> Create(CropCreateDto dto, Guid userId)
        {
            // Get farmer from userId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == userId &&
                   !f.IsDeleted,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Check if address already exists for this farmer
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c => 
                    c.Address == dto.Address && 
                    c.CreatedBy == farmer.FarmerId && 
                    (c.IsDeleted == null || c.IsDeleted == false),
                asNoTracking: true
            );

            if (existingCrop != null)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ này đã được sử dụng cho vùng trồng khác. Vui lòng chọn địa chỉ khác."
                );
            }

            // Generate CropCode using code generator
            var cropCode = await _codeGenerator.GenerateCropCodeAsync();

            // Create new crop
            var newCrop = dto.MapToCrop(farmer.FarmerId, cropCode);

            await _unitOfWork.CropRepository.CreateAsync(newCrop);
            await _unitOfWork.SaveChangesAsync();

            var cropDto = newCrop.MapToCropViewAllDto();

            return new ServiceResult(
                Const.SUCCESS_CREATE_CODE,
                Const.SUCCESS_CREATE_MSG,
                cropDto
            );
        }

        public async Task<IServiceResult> Update(CropUpdateDto dto, Guid userId)
        {
            // Get farmer from userId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == userId &&
                   !f.IsDeleted,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get current crop and check ownership
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == dto.CropId &&
                   c.IsDeleted == false &&
                   c.CreatedBy == farmer.FarmerId,
                asNoTracking: false
            );

            if (existingCrop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền chỉnh sửa."
                );
            }

            // Check if new address already exists for this farmer (excluding current crop)
            if (existingCrop.Address != dto.Address)
            {
                var duplicateCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => 
                        c.Address == dto.Address && 
                        c.CreatedBy == farmer.FarmerId && 
                        c.CropId != dto.CropId &&
                        (c.IsDeleted == null || c.IsDeleted == false),
                    asNoTracking: true
                );

                if (duplicateCrop != null)
                {
                    return new ServiceResult(
                        Const.ERROR_EXCEPTION,
                        "Địa chỉ này đã được sử dụng cho vùng trồng khác. Vui lòng chọn địa chỉ khác."
                    );
                }
            }

            // Update crop
            dto.MapToCrop(existingCrop, farmer.FarmerId);

            await _unitOfWork.CropRepository.UpdateAsync(existingCrop);
            await _unitOfWork.SaveChangesAsync();

            var cropDto = existingCrop.MapToCropViewAllDto();

            return new ServiceResult(
                Const.SUCCESS_UPDATE_CODE,
                Const.SUCCESS_UPDATE_MSG,
                cropDto
            );
        }

        public async Task<IServiceResult> Delete(Guid cropId, Guid userId)
        {
            // Get farmer from userId
            var farmer = await _unitOfWork.FarmerRepository.GetByIdAsync(
                predicate: f =>
                   f.UserId == userId &&
                   !f.IsDeleted,
                asNoTracking: false
            );

            if (farmer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Get current crop and check ownership
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   c.IsDeleted == false &&
                   c.CreatedBy == farmer.FarmerId,
                asNoTracking: false
            );

            if (existingCrop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Crop hoặc bạn không có quyền xóa."
                );
            }

            // Soft delete crop
            existingCrop.IsDeleted = true;
            existingCrop.UpdatedAt = DateTime.UtcNow;
            existingCrop.UpdatedBy = farmer.FarmerId;

            await _unitOfWork.CropRepository.UpdateAsync(existingCrop);
            await _unitOfWork.SaveChangesAsync();

            return new ServiceResult(
                Const.SUCCESS_DELETE_CODE,
                Const.SUCCESS_DELETE_MSG
            );
        }

        // Chuyển đổi status crop theo yêu cầu
        public async Task<IServiceResult> TransitionStatus(Guid cropId, string targetStatus)
        {
            try
            {
                // Get crop
                var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => c.CropId == cropId && (!c.IsDeleted.HasValue || !c.IsDeleted.Value),
                    asNoTracking: false
                );

                if (crop == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy crop hoặc crop đã bị xóa."
                    );
                }

                // Check transition logic
                if (!CanTransitionTo(crop.Status, targetStatus))
                {
                    return new ServiceResult(
                        Const.ERROR_EXCEPTION,
                        $"Không thể chuyển từ {crop.Status} sang {targetStatus}."
                    );
                }

                // Update status
                var oldStatus = crop.Status;
                crop.Status = targetStatus;
                crop.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.CropRepository.UpdateAsync(crop);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(
                    Const.SUCCESS_UPDATE_CODE,
                    $"Đã chuyển status từ {oldStatus} sang {targetStatus} thành công."
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    $"Lỗi khi chuyển đổi status: {ex.Message}"
                );
            }
        }

        // Kiểm tra logic chuyển đổi status
        private bool CanTransitionTo(string currentStatus, string targetStatus)
        {
            return currentStatus switch
            {
                "Active" => targetStatus == "Harvested" || targetStatus == "Inactive",
                "Inactive" => targetStatus == "Active",
                "Harvested" => targetStatus == "Processed" || targetStatus == "Active",
                "Processed" => targetStatus == "Sold" || targetStatus == "Active",
                "Sold" => targetStatus == "Active",
                "Other" => targetStatus == "Active",
                _ => false
            };
        }

        // Tự động chuyển status theo workflow
        public async Task<IServiceResult> AutoTransitionStatus(Guid cropId)
        {
            try
            {
                // Get crop
                var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                    predicate: c => c.CropId == cropId && (!c.IsDeleted.HasValue || !c.IsDeleted.Value),
                    asNoTracking: false
                );

                if (crop == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy crop hoặc crop đã bị xóa."
                    );
                }

                // Get next status
                var nextStatus = GetNextStatus(crop.Status);
                if (nextStatus == null)
                {
                    return new ServiceResult(
                        Const.ERROR_EXCEPTION,
                        $"Không thể auto transition từ status {crop.Status}."
                    );
                }

                // Update status
                var oldStatus = crop.Status;
                crop.Status = nextStatus;
                crop.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.CropRepository.UpdateAsync(crop);
                await _unitOfWork.SaveChangesAsync();

                return new ServiceResult(
                    Const.SUCCESS_UPDATE_CODE,
                        $"Đã auto transition status từ {oldStatus} sang {nextStatus} thành công."
                );
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                        $"Lỗi khi auto transition status: {ex.Message}"
                );
            }
        }

        // Lấy status tiếp theo
        private string GetNextStatus(string currentStatus)
        {
            return currentStatus switch
            {
                "Active" => "Harvested",
                "Harvested" => "Processed", 
                "Processed" => "Sold",
                "Sold" => "Active",
                "Inactive" => "Active",
                "Other" => "Active",
                _ => null
            };
        }
    }
}
