using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Mappers;
using Microsoft.EntityFrameworkCore;

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
                include: c => c.Include(c => c.CoffeeTypeParent),
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

        public async Task<IServiceResult> GetById(Guid typeId)
        {
            // Tìm tài coffee type theo ID
            var type = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(
                predicate: u => u.CoffeeTypeId == typeId,
                include: c => c.Include ( c => c.CoffeeTypeParent),
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

        public async Task<IServiceResult> DeleteById(Guid typeId)
        {
            try
            {
                // Tìm coffee type theo ID từ repository
                var type = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(
                    predicate: u => u.CoffeeTypeId == typeId,
                    asNoTracking: true
                );

                // Nếu không tìm thấy coffee type, trả về cảnh báo không có dữ liệu
                if (type == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa coffee type ra khỏi repository
                    await _unitOfWork.CoffeeTypeRepository.RemoveAsync(type);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteById(Guid typeId)
        {
            try
            {
                // Tìm coffee type theo ID từ repository
                var type = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(
                    predicate: u => u.CoffeeTypeId == typeId,
                    asNoTracking: true
                );

                // Nếu không tìm thấy coffee type, trả về cảnh báo không có dữ liệu
                if (type == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu xoá mềm bằng IsDeleted
                    type.IsDeleted = true;
                    type.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm ở repository
                    await _unitOfWork.CoffeeTypeRepository.UpdateAsync(type);

                    // Lưu thay đổi vào database
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem việc lưu có thành công không
                    if (result > 0)
                    {
                        return new ServiceResult(
                            Const.SUCCESS_DELETE_CODE,
                            Const.SUCCESS_DELETE_MSG
                        );
                    }
                    else
                    {
                        return new ServiceResult(
                            Const.FAIL_DELETE_CODE,
                            Const.FAIL_DELETE_MSG
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> Create(CoffeeTypeCreateDto coffeeTypeDto)
        {
            try
            {                
                // Generate coffee type code
                string typeCode = await _codeGenerator.GenerateCoffeeTypeCodeAsync();

                // Map DTO to Entity
                var newCoffeeType = coffeeTypeDto.MapToCofeeTypeCreateDto(typeCode);

                // Tạo CoffeeType ở repository
                await _unitOfWork.CoffeeTypeRepository.CreateAsync(newCoffeeType);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = newCoffeeType.MapToCoffeeTypeViewAllDto();

                    return new ServiceResult(
                        Const.SUCCESS_CREATE_CODE,
                        Const.SUCCESS_CREATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        Const.FAIL_CREATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> Update(CoffeeTypeUpdateDto coffeeTypeDto)
        {
            try
            {
                // Kiểm tra coffee type tồn tại
                var coffeeType = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(coffeeTypeDto.CoffeeTypeId);

                if (coffeeType == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Coffee type không tồn tại."
                    );
                }

                // Đây là trường hợp nếu coffee có category là general thì set null cho parentID
                if (coffeeTypeDto.CoffeeTypeParentId == Guid.Empty)
                    coffeeTypeDto.CoffeeTypeParentId = null;

                //Map DTO to Entity
                coffeeTypeDto.MapToUpdateCoffeeType(coffeeType);

                // Cập nhật Coffee type ở repository
                await _unitOfWork.CoffeeTypeRepository.UpdateAsync(coffeeType);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = coffeeType.MapToCoffeeTypeViewAllDto();

                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> UpdateStatus(CoffeeTypeUpdateStatusDto coffeeTypeDto)
        {
            try
            {
                // Kiểm tra coffee type tồn tại
                var coffeeType = await _unitOfWork.CoffeeTypeRepository.GetByIdAsync(coffeeTypeDto.CoffeeTypeId);

                if (coffeeType == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Coffee type không tồn tại."
                    );
                }

                coffeeType.Status = coffeeTypeDto.Status.ToString();

                // Cập nhật Coffee type ở repository
                await _unitOfWork.CoffeeTypeRepository.UpdateAsync(coffeeType);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Map the saved entity to a response DTO
                    var responseDto = coffeeType.MapToCoffeeTypeViewAllDto();

                    return new ServiceResult(
                        Const.SUCCESS_UPDATE_CODE,
                        Const.SUCCESS_UPDATE_MSG,
                        responseDto
                    );
                }
                else
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        Const.FAIL_UPDATE_MSG
                    );
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
