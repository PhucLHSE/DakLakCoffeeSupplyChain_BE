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

        public async Task<IServiceResult> Create(ContractItemCreateDto contractItemDto, Guid userId)
        {
            try
            {
                // Kiểm tra BusinessManager có tồn tại hay không
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

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
                        "Loại cà phê này đã có trong hợp đồng."
                    );
                }

                // Lấy danh sách item hiện có của contract
                var existingItems = await _unitOfWork.ContractItemRepository.GetAllAsync(
                    predicate: ci => 
                       ci.ContractId == contractItemDto.ContractId && 
                       !ci.IsDeleted,
                    asNoTracking: true
                );

                // Giới hạn SỐ LƯỢNG theo Contracts.TotalQuantity
                double contractQtyCap = contract.TotalQuantity.HasValue 
                    ? Convert.ToDouble(contract.TotalQuantity.Value) : 0d;

                if (contractQtyCap > 0d)
                {
                    double existingTotalQty = existingItems?.Sum(ci => Convert.ToDouble(ci.Quantity)) ?? 0d;
                    double requestedQty = Convert.ToDouble(contractItemDto.Quantity ?? 0);
                    const double epsQty = 1e-6;

                    if ((existingTotalQty + requestedQty) - contractQtyCap > epsQty)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Tổng số lượng vượt giới hạn hợp đồng: hiện có {existingTotalQty:0.##} kg + thêm {requestedQty:0.##} kg > {contractQtyCap:0.##} kg."
                        );
                    }
                }

                // Giới hạn GIÁ TRỊ theo Contracts.TotalValue
                decimal contractValueCap = contract.TotalValue.HasValue
                    ? Convert.ToDecimal(contract.TotalValue.Value) : 0m;

                if (contractValueCap > 0m)
                {
                    // helper tính giá trị dòng (làm tròn 2 số)
                    static decimal LineNet(decimal qty, decimal unitPrice, decimal discountPct)
                    {
                        if (discountPct < 0m) discountPct = 0m;
                        if (discountPct > 100m) discountPct = 100m;
                        var net = qty * unitPrice * (1 - discountPct / 100m);

                        return Math.Round(net, 2, MidpointRounding.AwayFromZero);
                    }

                    decimal existingTotalValue = 0m;

                    if (existingItems != null)
                    {
                        foreach (var ci in existingItems)
                        {
                            decimal q = Convert.ToDecimal(ci.Quantity);
                            decimal p = Convert.ToDecimal(ci.UnitPrice);
                            decimal d = Convert.ToDecimal(ci.DiscountAmount ?? 0); // percent
                            existingTotalValue += LineNet(q, p, d);
                        }
                    }

                    decimal reqQty = Convert.ToDecimal(contractItemDto.Quantity ?? 0);
                    decimal reqPrice = Convert.ToDecimal(contractItemDto.UnitPrice ?? 0);
                    decimal reqDisc = Convert.ToDecimal(contractItemDto.DiscountAmount ?? 0);
                    decimal newLineValue = LineNet(reqQty, reqPrice, reqDisc);

                    decimal totalWithNew = existingTotalValue + newLineValue;

                    // epsilon cho tiền tệ (0.01 ~ 1 xu)
                    const decimal epsMoney = 0.01m;

                    if (totalWithNew - contractValueCap > epsMoney)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Tổng giá trị các mặt hàng vượt Tổng giá trị hợp đồng: hiện có {existingTotalValue:N0} + thêm {newLineValue:N0} = {totalWithNew:N0} VND > {contractValueCap:N0} VND."
                        );
                    }
                }

                // Sinh mã định danh cho ContractItem
                string contractItemCode = await _codeGenerator
                    .GenerateContractItemCodeAsync(contractItemDto.ContractId ?? Guid.NewGuid());

                // Ánh xạ dữ liệu từ DTO vào entity
                var newContractItem = contractItemDto
                    .MapToNewContractItem(contractItemCode);

                // Tạo ContractItem ở repository
                await _unitOfWork.ContractItemRepository
                    .CreateAsync(newContractItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu để trả về
                    var createdItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                        predicate: ci => ci.ContractItemId == newContractItem.ContractItemId,
                        include: query => query
                           .Include(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (createdItem != null)
                    {
                        // Map sang DTO chi tiết để trả về
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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> Update(ContractItemUpdateDto contractItemDto, Guid userId)
        {
            try
            {
                // Kiểm tra BusinessManager theo userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: 
                       m => m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

                // Tìm contractItem theo ID
                var contractItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                    predicate: ci => 
                       ci.ContractItemId == contractItemDto.ContractItemId &&
                       !ci.IsDeleted,
                    asNoTracking: false
                );

                // Nếu không tìm thấy
                if (contractItem == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm thấy mục hợp đồng cần cập nhật."
                    );
                }

                // Xác định Contract đích (giữ nguyên nếu DTO null)
                var targetContractId = contractItemDto.ContractId != Guid.Empty
                    ? contractItemDto.ContractId
                    : contractItem.ContractId;

                // Validate Contract tồn tại
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c => 
                       c.ContractId == targetContractId && 
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

                // Lấy danh sách item khác (exclude chính nó) để tính tổng
                var otherItems = await _unitOfWork.ContractItemRepository.GetAllAsync(
                    predicate: ci =>
                       ci.ContractId == targetContractId &&
                       ci.ContractItemId != contractItemDto.ContractItemId &&
                       !ci.IsDeleted,
                    asNoTracking: true
                );

                // Giới hạn SỐ LƯỢNG theo Contracts.TotalQuantity
                double qtyCap = contract.TotalQuantity.HasValue 
                    ? Convert.ToDouble(contract.TotalQuantity.Value) : 0d;

                if (qtyCap > 0d)
                {
                    double othersQty = otherItems?.Sum(ci => Convert.ToDouble(ci.Quantity)) ?? 0d;
                    double reqQty = Convert.ToDouble(contractItemDto.Quantity ?? 0);
                    const double epsQty = 1e-6;

                    if ((othersQty + reqQty) - qtyCap > epsQty)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Tổng số lượng vượt giới hạn hợp đồng: hiện có {othersQty:0.##} kg + cập nhật {reqQty:0.##} kg > {qtyCap:0.##} kg."
                        );
                    }
                }

                // Giới hạn GIÁ TRỊ theo Contracts.TotalValue
                decimal valueCap = contract.TotalValue.HasValue
                    ? Convert.ToDecimal(contract.TotalValue.Value) : 0m;

                if (valueCap > 0m)
                {
                    static decimal LineNet(decimal qty, decimal unitPrice, decimal discountPct)
                    {
                        if (discountPct < 0m) discountPct = 0m;
                        if (discountPct > 100m) discountPct = 100m;
                        var net = qty * unitPrice * (1 - discountPct / 100m);
                        return Math.Round(net, 2, MidpointRounding.AwayFromZero);
                    }

                    decimal othersValue = 0m;

                    if (otherItems != null)
                    {
                        foreach (var ci in otherItems)
                        {
                            decimal q = Convert.ToDecimal(ci.Quantity);
                            decimal p = Convert.ToDecimal(ci.UnitPrice);
                            decimal d = Convert.ToDecimal(ci.DiscountAmount ?? 0); // %
                            othersValue += LineNet(q, p, d);
                        }
                    }

                    decimal rq = Convert.ToDecimal(contractItemDto.Quantity ?? 0);
                    decimal rp = Convert.ToDecimal(contractItemDto.UnitPrice ?? 0);
                    decimal rd = Convert.ToDecimal(contractItemDto.DiscountAmount ?? 0);
                    decimal newLine = LineNet(rq, rp, rd);

                    decimal totalWithUpdate = othersValue + newLine;
                    const decimal epsMoney = 0.01m;

                    if (totalWithUpdate - valueCap > epsMoney)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Tổng giá trị các mặt hàng vượt Tổng giá trị hợp đồng: hiện có {othersValue:N0} + dòng cập nhật {newLine:N0} = {totalWithUpdate:N0} VND > {valueCap:N0} VND."
                        );
                    }
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                contractItemDto.MapToUpdateContractItem(contractItem);

                // Bảo đảm ContractId đích (phòng trường hợp DTO để null)
                contractItem.ContractId = targetContractId;

                // Cập nhật contractItem ở repository
                await _unitOfWork.ContractItemRepository
                    .UpdateAsync(contractItem);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại entity sau khi cập nhật (kèm CoffeeType)
                    var updatedItem = await _unitOfWork.ContractItemRepository.GetByIdAsync(
                        predicate: ci => 
                           ci.ContractItemId == contractItemDto.ContractItemId &&
                           !ci.IsDeleted,
                        include: query => query
                           .Include(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (updatedItem != null)
                    {
                        // Map sang DTO chi tiết để trả về
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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình xóa
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> DeleteContractItemById(Guid contractItemId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager theo userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                        m.UserId == userId &&
                        !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

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
                    await _unitOfWork.ContractItemRepository
                        .RemoveAsync(contractItem);

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

        public async Task<IServiceResult> SoftDeleteContractItemById(Guid contractItemId, Guid userId)
        {
            try
            {
                // Tìm BusinessManager theo userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                        m.UserId == userId &&
                        !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy BusinessManager tương ứng với tài khoản."
                    );
                }

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
                    await _unitOfWork.ContractItemRepository
                        .UpdateAsync(contractItem);

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
