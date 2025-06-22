using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class CoffeeTypeService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator) : ICoffeeTypeService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICodeGenerator _codeGenerator = codeGenerator;

        public async Task<IServiceResult> GetAll()
        {
            // Truy vấn tất cả coffee type từ repository
            var coffeeTypes = await _unitOfWork.CoffeeTypeRepository.GetAllAsync(
                predicate: u => u.IsDeleted != true,
                orderBy: u => u.OrderBy(u => u.TypeCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (coffeeTypes == null || coffeeTypes.Count == 0)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<CoffeeTypeViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var coffeeTypeDto = coffeeTypes
                    .Select(coffeeTypes => coffeeTypes.MapToCoffeeTypeViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    coffeeTypeDto
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid TypeId)
        {
            // Tìm tài coffee type theo ID
            var type = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(
                predicate: u => u.CoffeeTypeId == TypeId,
                asNoTracking: true
            );

            // Trả về cảnh báo nếu không tìm thấy
            if (type == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new CoffeeTypeViewAllDto()   // Trả về DTO rỗng
                );
            }
            else
            {
                // Map entity sang DTO chi tiết
                var coffeeTypeDto = type.MapToCoffeeTypeViewAllDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    coffeeTypeDto
                );
            }
        }
    }
}
