using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
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
    public class CropService : ICropService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private const string ERROR_FARMER_NOT_FOUND_MSG = "Không tìm thấy Farmer tương ứng với tài khoản.";

        public CropService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            // Lấy Farmer hiện tại từ userId
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
                    ERROR_FARMER_NOT_FOUND_MSG
                );
            }

            // Lấy danh sách Crop từ repository
            var crops = await _unitOfWork.CropRepository.GetAllAsync(
                predicate: c => 
                   (!c.IsDeleted.HasValue || !c.IsDeleted.Value) && 
                   c.CreatedBy == farmer.FarmerId,
                orderBy: query => query.OrderBy(c => c.CreatedAt),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (crops == null ||
                !crops.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CropViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
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
            // Lấy Farmer hiện tại từ userId
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
                    ERROR_FARMER_NOT_FOUND_MSG
                );
            }

            // Lấy Crop theo ID và kiểm tra quyền sở hữu
            var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   (!c.IsDeleted.HasValue || !c.IsDeleted.Value) &&
                   c.CreatedBy == farmer.FarmerId,
                include: query => query
                    .Include(c => c.CreatedByNavigation)
                        .ThenInclude(f => f.User)
                    .Include(c => c.UpdatedByNavigation)
                        .ThenInclude(f => f.User),
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
            // Kiểm tra địa chỉ có thuộc Đắk Lắk không
            var address = dto.Address.ToLower();
            var isDakLakAddress = address.Contains("đắk lắk") || 
                                 address.Contains("dak lak") ||
                                 address.Contains("buôn ma thuột") ||
                                 address.Contains("buon ma thuot") ||
                                 address.Contains("ea ") ||
                                 address.Contains("krông") ||
                                 address.Contains("krong") ||
                                 address.Contains("cư ") ||
                                 address.Contains("cu ") ||
                                 address.Contains("lắk") ||
                                 address.Contains("lak") ||
                                 address.Contains("m'drắk") ||
                                 address.Contains("mdrak");

            if (!isDakLakAddress)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Địa chỉ phải thuộc khu vực Đắk Lắk."
                );
            }

            // Lấy Farmer hiện tại từ userId
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
                    ERROR_FARMER_NOT_FOUND_MSG
                );
            }

            // Tự động tạo CropCode
            var cropCode = await _codeGenerator.GenerateCropCodeAsync();

            // Kiểm tra CropCode đã tồn tại chưa (để đảm bảo không trùng)
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c => c.CropCode == cropCode && c.IsDeleted != true,
                asNoTracking: true
            );

            if (existingCrop != null)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "CropCode đã tồn tại trong hệ thống."
                );
            }

            // Tạo Crop mới với CropCode đã được tạo
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
            // Lấy Farmer hiện tại từ userId
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
                    ERROR_FARMER_NOT_FOUND_MSG
                );
            }

            // Lấy Crop hiện tại và kiểm tra quyền sở hữu
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == dto.CropId &&
                   (!c.IsDeleted.HasValue || !c.IsDeleted.Value) &&
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

            // Kiểm tra CropCode đã tồn tại chưa (trừ crop hiện tại)
            var duplicateCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c => c.CropCode == dto.CropCode && c.CropId != dto.CropId && c.IsDeleted != true,
                asNoTracking: true
            );

            if (duplicateCrop != null)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "CropCode đã tồn tại trong hệ thống."
                );
            }

            // Cập nhật Crop
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
            // Lấy Farmer hiện tại từ userId
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
                    ERROR_FARMER_NOT_FOUND_MSG
                );
            }

            // Lấy Crop hiện tại và kiểm tra quyền sở hữu
            var existingCrop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   (!c.IsDeleted.HasValue || !c.IsDeleted.Value) &&
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

            // Kiểm tra xem Crop có đang được sử dụng trong CropSeasonDetail không
            var hasCropSeasonDetails = await _unitOfWork.CropSeasonDetailRepository.GetAllAsync(
                predicate: csd => csd.CropId == cropId && !csd.IsDeleted,
                asNoTracking: true
            );

            if (hasCropSeasonDetails.Any())
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    "Không thể xóa Crop vì đang được sử dụng trong CropSeasonDetail."
                );
            }

            // Soft delete
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
    }
}
