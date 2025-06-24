using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Services.Mappers;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractItemService : IContractItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ContractItemService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> Create(ContractItemCreateDto contractItemDto)
        {
            try
            {
                // Kiểm tra Contract có tồn tại không
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c => 
                       c.ContractId == contractItemDto.ContractId && 
                       !c.IsDeleted,
                    asNoTracking: true
                );

                if (contract == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Không tìm thấy hợp đồng tương ứng."
                    );
                }

                // Kiểm tra đã có loại cà phê này trong hợp đồng chưa
                var isDuplicated = await _unitOfWork.ContractItemRepository.AnyAsync(
                    predicate: ci => 
                       ci.ContractId == contractItemDto.ContractId &&
                       ci.CoffeeTypeId == contractItemDto.CoffeeTypeId &&
                       !ci.IsDeleted
                );

                if (isDuplicated)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE, 
                        "Loại cà phê này đã có trong hợp đồng.");
                }


                // Sinh mã định danh cho ContractItem
                string contractItemCode = await _codeGenerator.GenerateContractItemCodeAsync(contractItemDto.ContractId);

                // Map DTO to Entity
                var newContractItem = contractItemDto.MapToNewContractItem(contractItemCode);

                // Tạo ContractItem ở repository
                await _unitOfWork.ContractItemRepository.CreateAsync(newContractItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    var createdItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                        predicate: ci => ci.ContractItemId == newContractItem.ContractItemId,
                        include: query => query
                           .Include(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (createdItem != null)
                    {
                        var responseDto = createdItem.MapToContractItemViewDto();

                        return new ServiceResult(
                            Const.SUCCESS_CREATE_CODE,
                            Const.SUCCESS_CREATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Tạo mới thành công nhưng không truy xuất được dữ liệu."
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

        public async Task<IServiceResult> Update(ContractItemUpdateDto contractItemDto)
        {
            try
            {
                // Tìm contractItem theo ID
                var contractItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                    predicate: ci => 
                       ci.ContractItemId == contractItemDto.ContractItemId &&
                       !ci.IsDeleted,
                    include: query => query
                           .Include(ci => ci.CoffeeType)
                           .Include(ci => ci.Contract),
                    asNoTracking: true
                );

                // Nếu không tìm thấy
                if (contractItem == null || contractItem.IsDeleted)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy mục hợp đồng cần cập nhật."
                    );
                }

                // Kiểm tra loại cà phê có bị trùng trong hợp đồng không (trừ chính nó)
                var isDuplicated = await _unitOfWork.ContractItemRepository.AnyAsync(
                    predicate: ci =>
                        ci.ContractItemId != contractItemDto.ContractItemId &&
                        ci.ContractId == contractItemDto.ContractId &&
                        ci.CoffeeTypeId == contractItemDto.CoffeeTypeId &&
                        !ci.IsDeleted
                );

                if (isDuplicated)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Loại cà phê này đã có trong hợp đồng."
                    );
                }

                //Map DTO to Entity
                contractItemDto.MapToUpdateContractItem(contractItem);

                // Cập nhật contractItem ở repository
                await _unitOfWork.ContractItemRepository.UpdateAsync(contractItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại entity sau khi cập nhật (kèm CoffeeType)
                    var updatedItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                        predicate: ci => ci.ContractItemId == contractItemDto.ContractItemId,
                        include: query => query
                           .Include(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (updatedItem != null)
                    {
                        var responseDto = updatedItem.MapToContractItemViewDto();

                        return new ServiceResult(
                            Const.SUCCESS_UPDATE_CODE,
                            Const.SUCCESS_UPDATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Cập nhật thành công nhưng không truy xuất được dữ liệu."
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

        public async Task<IServiceResult> DeleteContractItemById(Guid contractItemId)
        {
            try
            {
                // Tìm contractItem theo ID
                var contractItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                    predicate: ct => 
                       ct.ContractItemId == contractItemId && 
                       !ct.IsDeleted,
                    include: query => query
                           .Include(ct => ct.CoffeeType)
                           .Include(ct => ct.Contract),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa contractItem khỏi repository
                    await _unitOfWork.ContractItemRepository.RemoveAsync(contractItem);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
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
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> SoftDeleteContractItemById(Guid contractItemId)
        {
            try
            {
                // Tìm contractItem theo ID
                var contractItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                    predicate: ct => 
                       ct.ContractItemId == contractItemId && 
                       !ct.IsDeleted,
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractItem == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu contractItem là đã xóa
                    contractItem.IsDeleted = true;
                    contractItem.UpdatedAt = DateHelper.NowVietnamTime();

                    // Cập nhật xoá mềm contractItem ở repository
                    await _unitOfWork.ContractItemRepository.UpdateAsync(contractItem);

                    // Lưu thay đổi
                    var result = await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra kết quả
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
                // Trả về lỗi nếu có exception
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }
    }
}
