using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;
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
    public class CropService : ICropService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CropService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
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
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Lấy danh sách Crop từ repository
            var crops = await _unitOfWork.CropRepository.GetAllAsync(
                predicate: c => 
                   c.IsDeleted == false && 
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
                    "Không tìm thấy Farmer tương ứng với tài khoản."
                );
            }

            // Tìm Crop theo ID
            var crop = await _unitOfWork.CropRepository.GetByIdAsync(
                predicate: c =>
                   c.CropId == cropId &&
                   c.CreatedBy == farmer.FarmerId &&
                   c.IsDeleted == false,
                include: query => query
                   .Include(c => c.CreatedByNavigation)
                      .ThenInclude(cbn => cbn.User)
                   .Include(c => c.UpdatedByNavigation)
                      .ThenInclude(cbn => cbn.User),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy crop
            if (crop == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new CropViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var cropDto = crop.MapToCropViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    cropDto
                );
            }
        }
    }
}
