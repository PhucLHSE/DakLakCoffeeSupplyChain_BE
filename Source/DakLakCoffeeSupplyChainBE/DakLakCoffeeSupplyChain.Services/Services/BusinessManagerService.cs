using DakLakCoffeeSupplyChain.Common.DTOs.ProductDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class BusinessManagerService : IBussinessManagerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BusinessManagerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IServiceResult> GetAll()
        {
            // Lấy danh sách quản lý doanh nghiệp từ repository
            var businessManagers = await _unitOfWork.BusinessManagerRepository.GetAllAsync(
                predicate: bm => bm.IsDeleted != true,
                include: query => query
                   .Include(bm => bm.User),
                orderBy: query => query.OrderBy(bm => bm.ManagerCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (businessManagers == null || !businessManagers.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ProductViewAllDto>()  // Trả về danh sách rỗng
                );
            }
            else
            {
                // Chuyển đổi sang danh sách DTO để trả về cho client
                var businessManagerDtos = businessManagers
                    .Select(businessManagers => businessManagers.MapToBusinessManagerViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    businessManagerDtos
                );
            }
        }
    }
}
