using DakLakCoffeeSupplyChain.Common.DTOs.BusinessManagerDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.BusinessBuyerDTOs;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class BusinessBuyerService : IBusinessBuyerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BusinessBuyerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => m.UserId == userId,
                asNoTracking: true
            );

            if (manager == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy BusinessManager tương ứng với tài khoản."
                );
            }

            // Lấy danh sách buyer từ repository
            var businessBuyers = await _unitOfWork.BusinessBuyerRepository.GetAllAsync(
                predicate: bb => bb.IsDeleted != true && bb.CreatedBy == manager.ManagerId,
                include: query => query
                   .Include(bb => bb.CreatedByNavigation),
                orderBy: query => query.OrderBy(bb => bb.BuyerCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (businessBuyers == null || !businessBuyers.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<BusinessBuyerViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var businessBuyerDtos = businessBuyers
                    .Select(businessBuyers => businessBuyers.MapToBusinessBuyerViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessBuyerDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid buyerId)
        {
            // Tìm buyer theo ID
            var businessBuyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                predicate: bb => bb.BuyerId == buyerId,
                include: query => query
                   .Include(bb => bb.CreatedByNavigation),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy buyer
            if (businessBuyer == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new BusinessBuyerViewDetailDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Map sang DTO chi tiết để trả về
                var businessBuyerDto = businessBuyer.MapToBusinessBuyerViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessBuyerDto
                );
            }
        }
    }
}
