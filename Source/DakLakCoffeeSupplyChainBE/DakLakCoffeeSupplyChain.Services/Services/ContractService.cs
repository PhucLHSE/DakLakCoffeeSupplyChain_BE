using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.UnitOfWork;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IUploadService _uploadService;

        public ContractService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator, IUploadService uploadService)
        {
            _unitOfWork = unitOfWork 
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
                
            _uploadService = uploadService
                ?? throw new ArgumentNullException(nameof(uploadService));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            // Lấy BusinessManager hiện tại từ userId
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

            // Truy vấn tất cả hợp đồng từ repository
            var contracts = await _unitOfWork.ContractRepository.GetAllAsync(
                predicate: c => 
                   !c.IsDeleted &&
                   c.SellerId == manager.ManagerId,
                include: query => query
                   .Include(c => c.Buyer)
                   .Include(c => c.Seller)
                      .ThenInclude(s => s.User),
                orderBy: c => c.OrderBy(c => c.ContractCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (contracts == null || 
                !contracts.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ContractViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var contractDtos = contracts
                    .Select(contracts => contracts.MapToContractViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid contractId, Guid userId)
        {
            // Lấy BusinessManager hiện tại từ userId
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

            // Tìm contract theo ID
            var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                predicate: c => 
                   c.ContractId == contractId &&
                   c.SellerId == manager.ManagerId &&
                   !c.IsDeleted,
                include: query => query
                   .Include(c => c.Buyer)
                   .Include(c => c.Seller)
                      .ThenInclude(s => s.User)
                   .Include(c => c.ContractItems.Where(ci => !ci.IsDeleted))
                      .ThenInclude(ci => ci.CoffeeType),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy contract
            if (contract == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ContractViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Sắp xếp danh sách ContractItems theo ContractItemCode tăng dần
                contract.ContractItems = contract.ContractItems
                    .OrderBy(ci => ci.ContractItemCode)
                    .ToList();

                // Map sang DTO chi tiết để trả về
                var contractDto = contract.MapToContractViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDto
                );
            }
        }

        public async Task<IServiceResult> Create(ContractCreateDto contractDto, Guid userId)
        {
            try
            {
                // Tìm BusinessManager tương ứng với userId
                var businessManager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    include: query => query
                       .Include(bm => bm.User)
                          .ThenInclude(u => u.Role),
                    asNoTracking: true
                );

                if (businessManager == null ||
                    businessManager.User == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không tìm thấy thông tin quản lý doanh nghiệp."
                    );
                }

                var user = businessManager.User;

                // Kiểm tra quyền hạn
                if (user.IsDeleted || 
                    user.Role == null || 
                    user.Role.RoleName != "BusinessManager")
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Người dùng không có quyền tạo hợp đồng."
                    );
                }

                var sellerId = businessManager.ManagerId;

                // Kiểm tra Buyer tồn tại
                var buyer = await _unitOfWork.BusinessBuyerRepository.GetByIdAsync(
                    predicate: b => 
                       b.BuyerId == contractDto.BuyerId && 
                       !b.IsDeleted,
                    asNoTracking: true
                );
                if (buyer == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy thông tin bên mua."
                    );
                }

                // Kiểm tra ContractNumber đã tồn tại chưa (của cùng Seller)
                bool isDuplicateContractNumber = await _unitOfWork.ContractRepository.AnyAsync(
                    predicate: c =>
                        !c.IsDeleted &&
                        c.ContractNumber == contractDto.ContractNumber &&
                        c.SellerId == sellerId
                );

                if (isDuplicateContractNumber)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Số hợp đồng [{contractDto.ContractNumber}] đã tồn tại trong hệ thống."
                    );
                }

                // Tính tổng Quantity (an toàn với nullable)
                double totalItemQuantity = contractDto.ContractItems
                    .Sum(i => i.Quantity ?? 0);

                // Tính tổng trị giá = qty * price * (1 - pct/100) (đều là nullable)
                double totalItemValue = contractDto.ContractItems.Sum(i =>
                {
                    double q = i.Quantity ?? 0d;
                    double p = i.UnitPrice ?? 0d;
                    double pct = i.DiscountAmount ?? 0d; // % giảm
                    if (pct < 0) pct = 0;
                    if (pct > 100) pct = 100;
                    return q * p * (1 - pct / 100d);
                });

                // So sánh với tổng của hợp đồng (nếu được nhập)
                if (contractDto.TotalQuantity.HasValue && 
                    totalItemQuantity > contractDto.TotalQuantity.Value)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Tổng khối lượng từ các dòng hợp đồng ({totalItemQuantity} kg) vượt quá tổng khối lượng hợp đồng đã khai báo ({contractDto.TotalQuantity} kg)."
                    );
                }

                if (contractDto.TotalValue.HasValue && 
                    totalItemValue > contractDto.TotalValue.Value)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Tổng trị giá từ các dòng hợp đồng ({totalItemValue:N0} VND) vượt quá tổng giá trị hợp đồng đã khai báo ({contractDto.TotalValue:N0} VND)."
                    );
                }

                // Upload file nếu có
                string? contractFileUrl = contractDto.ContractFileUrl;

                if (contractDto.ContractFile is { Length: > 0 })
                {
                    var upload = await _uploadService.UploadContractFileAsync(contractDto.ContractFile);
                    contractFileUrl = upload.Url;
                }
                else if (!string.IsNullOrWhiteSpace(contractDto.ContractFileUrl) && IsHttpUrl(contractDto.ContractFileUrl))
                {
                    try
                    {
                        var up = await _uploadService.UploadFromUrlAsync(contractDto.ContractFileUrl);
                        contractFileUrl = up.Url;
                    }
                    catch (Exception ex)
                    {
                        // log lỗi thật ra (ex.Message) để soi nguyên nhân (hotlink, 403, sai URL...)
                        contractFileUrl = contractDto.ContractFileUrl; // fallback: lưu nguyên URL
                    }
                }

                contractDto.ContractFileUrl = contractFileUrl;

                // Sinh mã định danh cho Contract
                string contractCode = await _codeGenerator
                    .GenerateContractCodeAsync();

                // Cập nhật ContractFileUrl nếu có upload file TRƯỚC KHI map
                if (contractFileUrl != null)
                {
                    contractDto.ContractFileUrl = contractFileUrl;
                }

                // Ánh xạ dữ liệu từ DTO vào entity (sau khi đã cập nhật ContractFileUrl)
                var newContract = contractDto
                    .MapToNewContract(sellerId, contractCode);

                // Tạo Contract ở repository
                await _unitOfWork.ContractRepository
                    .CreateAsync(newContract);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại hợp đồng vừa tạo để trả ra DTO
                    var createdContract = await _unitOfWork.ContractRepository.GetByIdAsync(
                        predicate: c => c.ContractId == newContract.ContractId,
                        include: query => query
                            .Include(c => c.Buyer)
                            .Include(c => c.Seller)
                               .ThenInclude(u => u.User)
                            .Include(c => c.ContractItems)
                               .ThenInclude(i => i.CoffeeType),
                        asNoTracking: true
                    );

                    if (createdContract != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdContract
                            .MapToContractViewDetailDto();

                        return new ServiceResult(
                            Const.SUCCESS_CREATE_CODE,
                            Const.SUCCESS_CREATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Tạo hợp đồng thành công nhưng không truy xuất được dữ liệu."
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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                var errorMessage = $"Lỗi khi tạo hợp đồng: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                
                // Log chi tiết hơn
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    errorMessage
                );
            }
        }

        public async Task<IServiceResult> Update(ContractUpdateDto contractDto)
        {
            try
            {
                // Tìm contract theo ID
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c =>
                       c.ContractId == contractDto.ContractId &&
                       !c.IsDeleted,
                    include: query => query
                       .Include(c => c.Buyer)
                       .Include(c => c.Seller)
                          .ThenInclude(s => s.User)
                       .Include(c => c.ContractItems)
                          .ThenInclude(ci => ci.CoffeeType),
                    asNoTracking: false
                );

                // Nếu không tìm thấy
                if (contract == null || 
                    contract.IsDeleted)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Không tìm hợp đồng cần cập nhật."
                    );
                }

                // Kiểm tra ContractNumber có bị trùng (nếu có thay đổi)
                if (!string.Equals(contract.ContractNumber, contractDto.ContractNumber, StringComparison.OrdinalIgnoreCase))
                {
                    bool isDuplicate = await _unitOfWork.ContractRepository.AnyAsync(
                        predicate: c =>
                            !c.IsDeleted &&
                            c.SellerId == contract.SellerId &&
                            c.ContractNumber == contractDto.ContractNumber &&
                            c.ContractId != contractDto.ContractId
                    );

                    if (isDuplicate)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Số hợp đồng [{contractDto.ContractNumber}] đã tồn tại trong hệ thống."
                        );
                    }
                }

                // Tính lại tổng Quantity và Value từ các ContractItemDto
                double totalItemQuantity = contractDto.ContractItems
                    .Sum(i => i.Quantity ?? 0);

                // Tính tổng trị giá = qty * price * (1 - pct/100) (đều là nullable)
                double totalItemValue = contractDto.ContractItems.Sum(i =>
                {
                    double q = i.Quantity ?? 0d;
                    double p = i.UnitPrice ?? 0d;
                    double pct = i.DiscountAmount ?? 0d; // % giảm
                    if (pct < 0) pct = 0;
                    if (pct > 100) pct = 100;
                    return q * p * (1 - pct / 100d);
                });

                if (contractDto.TotalQuantity.HasValue && 
                    totalItemQuantity > contractDto.TotalQuantity.Value)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Tổng khối lượng từ các dòng hợp đồng ({totalItemQuantity} kg) vượt quá tổng khối lượng hợp đồng đã khai báo ({contractDto.TotalQuantity} kg)."
                    );
                }

                if (contractDto.TotalValue.HasValue && 
                    totalItemValue > contractDto.TotalValue.Value)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Tổng trị giá từ các dòng hợp đồng ({totalItemValue:N0} VND) vượt quá tổng giá trị hợp đồng đã khai báo ({contractDto.TotalValue:N0} VND)."
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                contract.MapToUpdateContract(contractDto);

                // Đồng bộ lại ContractItems
                var now = DateHelper.NowVietnamTime();

                var dtoItemIds = contractDto.ContractItems
                    .Select(i => i.ContractItemId).ToHashSet();

                // Xoá mềm những cái không còn
                foreach (var oldItem in contract.ContractItems)
                {
                    if (!dtoItemIds.Contains(oldItem.ContractItemId))
                    {
                        oldItem.IsDeleted = true;
                        oldItem.UpdatedAt = now;

                        await _unitOfWork.ContractItemRepository
                            .UpdateAsync(oldItem);
                    }
                }

                // Thêm mới hoặc cập nhật
                foreach (var itemDto in contractDto.ContractItems)
                {
                    if (itemDto.ContractItemId != Guid.Empty)
                    {
                        var existingItem = contract.ContractItems
                            .FirstOrDefault(i => i.ContractItemId == itemDto.ContractItemId);

                        if (existingItem != null)
                        {
                            existingItem.CoffeeTypeId = itemDto.CoffeeTypeId;
                            existingItem.Quantity = itemDto.Quantity;
                            existingItem.UnitPrice = itemDto.UnitPrice;
                            existingItem.DiscountAmount = itemDto.DiscountAmount;
                            existingItem.Note = itemDto.Note;
                            existingItem.UpdatedAt = now;
                            existingItem.IsDeleted = false;

                            await _unitOfWork.ContractItemRepository
                                .UpdateAsync(existingItem);
                        }
                    }
                    else
                    {
                        var newItem = new ContractItem
                        {
                            ContractItemId = Guid.NewGuid(),
                            ContractItemCode = $"CTI-{contract.ContractItems.Count + 1:D3}-{contract.ContractCode}",
                            CoffeeTypeId = itemDto.CoffeeTypeId,
                            Quantity = itemDto.Quantity,
                            UnitPrice = itemDto.UnitPrice,
                            DiscountAmount = itemDto.DiscountAmount,
                            Note = itemDto.Note,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsDeleted = false,
                            ContractId = contract.ContractId
                        };

                        await _unitOfWork.ContractItemRepository.CreateAsync(newItem);
                    }
                }

                // Cập nhật contract ở repository
                await _unitOfWork.ContractRepository
                    .UpdateAsync(contract);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại contract sau update để trả DTO
                    var updatedContract = await _unitOfWork.ContractRepository.GetByIdAsync(
                        predicate: c => 
                           c.ContractId == contractDto.ContractId &&
                           !c.IsDeleted,
                        include: query => query
                            .Include(c => c.Buyer)
                            .Include(c => c.Seller)
                               .ThenInclude(s => s.User)
                            .Include(c => c.ContractItems.Where(ci => !ci.IsDeleted))
                               .ThenInclude(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (updatedContract != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedContract.MapToContractViewDetailDto();

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
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                return new ServiceResult(
                    Const.ERROR_EXCEPTION,
                    ex.ToString()
                );
            }
        }

        public async Task<IServiceResult> DeleteContractById(Guid contractId)
        {
            try
            {
                // Tìm contract theo ID kèm danh sách ContractItems
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c => 
                       c.ContractId == contractId && 
                       !c.IsDeleted,
                    include: query => query
                        .Include(c => c.ContractItems.Where(ci => !ci.IsDeleted)),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contract == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa từng ContractItem trước (nếu có)
                    if (contract.ContractItems != null && 
                        contract.ContractItems.Any())
                    {
                        foreach (var item in contract.ContractItems.Where(i => !i.IsDeleted))
                        {
                            await _unitOfWork.ContractItemRepository
                                .RemoveAsync(item);
                        }
                    }

                    // Xóa contract khỏi repository
                    await _unitOfWork.ContractRepository
                        .RemoveAsync(contract);

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

        public async Task<IServiceResult> SoftDeleteContractById(Guid contractId)
        {
            try
            {
                // Tìm contract theo ID
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c => 
                       c.ContractId == contractId &&
                       !c.IsDeleted,
                    include: query => query
                       .Include(c => c.ContractItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contract == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu contract là đã xóa
                    contract.IsDeleted = true;
                    contract.UpdatedAt = DateHelper.NowVietnamTime();

                    // Đánh dấu tất cả ContractItems là đã xóa
                    foreach (var item in contract.ContractItems)
                    {
                        item.IsDeleted = true;
                        item.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của item
                        await _unitOfWork.ContractItemRepository
                            .UpdateAsync(item);
                    }

                    // Cập nhật xoá mềm contract ở repository
                    await _unitOfWork.ContractRepository
                        .UpdateAsync(contract);

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

        private static bool IsHttpUrl(string? u)
        {
            return Uri.TryCreate(u, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
