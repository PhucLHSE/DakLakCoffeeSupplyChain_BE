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
using DakLakCoffeeSupplyChain.Services.Mappers;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.Generators;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Services
{
    public class ContractDeliveryBatchService : IContractDeliveryBatchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public ContractDeliveryBatchService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            // Lấy ManagerId từ userId
            Guid? managerId = null;

            // Ưu tiên kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId &&
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager != null)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                // Nếu không phải Manager, kiểm tra BusinessStaff
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                    predicate: s => 
                       s.UserId == userId &&
                       !s.IsDeleted,
                    asNoTracking: true
                );

                if (staff != null)
                {
                    managerId = staff.SupervisorId;
                }
            }

            if (managerId == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                );
            }

            // Truy vấn trực tiếp lọc theo SellerId
            var contractDeliveryBatchs = await _unitOfWork.ContractDeliveryBatchRepository.GetAllAsync(
                predicate: cdb => 
                   !cdb.IsDeleted &&
                   cdb.Contract != null &&
                   cdb.Contract.SellerId == managerId,
                include: query => query
                   .Include(cdb => cdb.Contract),
                orderBy: cdb => cdb.OrderBy(cdb => cdb.DeliveryBatchCode),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (contractDeliveryBatchs == null || 
                !contractDeliveryBatchs.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<ContractDeliveryBatchViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var contractDeliveryBatchDtos = contractDeliveryBatchs
                    .Select(contractDeliveryBatch => contractDeliveryBatch.MapToContractDeliveryBatchViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDeliveryBatchDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid deliveryBatchId, Guid userId)
        {
            // Lấy ManagerId từ userId
            Guid? managerId = null;

            // Ưu tiên kiểm tra BusinessManager
            var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                predicate: m => 
                   m.UserId == userId && 
                   !m.IsDeleted,
                asNoTracking: true
            );

            if (manager != null)
            {
                managerId = manager.ManagerId;
            }
            else
            {
                // Nếu không phải Manager, kiểm tra BusinessStaff
                var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                    predicate: s => 
                       s.UserId == userId && 
                       !s.IsDeleted,
                    asNoTracking: true
                );

                if (staff != null)
                {
                    managerId = staff.SupervisorId;
                }
            }

            if (managerId == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    "Không xác định được Manager hoặc Supervisor từ userId."
                );
            }

            // Tìm contractDeliveryBatch theo ID
            var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                predicate: cdb =>
                   cdb.DeliveryBatchId == deliveryBatchId && 
                   !cdb.IsDeleted &&
                   cdb.Contract != null &&
                   cdb.Contract.SellerId == managerId,
                include: query => query
                   .Include(cdb => cdb.Contract)
                   .Include(cdb => cdb.ContractDeliveryItems.Where(cdi => !cdi.IsDeleted))
                      .ThenInclude(cdi => cdi.ContractItem)
                         .ThenInclude(ci => ci.CoffeeType),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy contractDeliveryBatch
            if (contractDeliveryBatch == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new ContractDeliveryBatchViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Sắp xếp danh sách ContractDeliveryItems theo DeliveryItemCode tăng dần
                contractDeliveryBatch.ContractDeliveryItems = contractDeliveryBatch.ContractDeliveryItems
                    .OrderBy(cdi => cdi.DeliveryItemCode)
                    .ToList();

                // Map sang DTO chi tiết để trả về
                var contractDto = contractDeliveryBatch.MapToContractDeliveryBatchViewDetailDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    contractDto
                );
            }
        }

        public async Task<IServiceResult> Create(ContractDeliveryBatchCreateDto contractDeliveryBatchDto, Guid userId)
        {
            try
            {
                Guid? managerId = null;

                // Ưu tiên kiểm tra BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s => 
                           s.UserId == userId && 
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Không tìm thấy quyền hợp lệ để tạo đợt giao hàng."
                    );
                }

                // Kiểm tra hợp đồng có tồn tại và thuộc về Manager (hoặc Manager của Staff)
                var contract = await _unitOfWork.ContractRepository.GetByIdAsync(
                    predicate: c => 
                       c.ContractId == contractDeliveryBatchDto.ContractId && 
                       c.SellerId == managerId && 
                       !c.IsDeleted,
                    include: q => q
                       .Include(c => c.ContractItems.Where(ci => !ci.IsDeleted))
                          .ThenInclude(ci => ci.CoffeeType)
                       .Include(c => c.ContractDeliveryBatches.Where(b => !b.IsDeleted)),
                    asNoTracking: true
                );

                if (contract == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Không tìm thấy hợp đồng hoặc không thuộc quyền quản lý."
                    );
                }

                // DeliveryRound đã tồn tại trong hợp đồng chưa
                bool isDeliveryRoundDuplicated = await _unitOfWork.ContractDeliveryBatchRepository.AnyAsync(
                    predicate: b =>
                       !b.IsDeleted &&
                       b.ContractId == contract.ContractId &&
                       b.DeliveryRound == contractDeliveryBatchDto.DeliveryRound
                );

                if (isDeliveryRoundDuplicated)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Đợt giao hàng số {contractDeliveryBatchDto.DeliveryRound} đã tồn tại trong hợp đồng này."
                    );
                }

                // ExpectedDeliveryDate phải nằm trong phạm vi ngày của hợp đồng
                if (contractDeliveryBatchDto.ExpectedDeliveryDate < contract.StartDate ||
                    contractDeliveryBatchDto.ExpectedDeliveryDate > contract.EndDate)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Ngày giao dự kiến phải nằm trong khoảng thời gian hiệu lực của hợp đồng: {contract.StartDate:dd/MM/yyyy} đến {contract.EndDate:dd/MM/yyyy}."
                    );
                }

                // TotalPlannedQuantity không vượt quá TotalQuantity của hợp đồng
                if (contract.TotalQuantity.HasValue && 
                    contractDeliveryBatchDto.TotalPlannedQuantity > contract.TotalQuantity.Value)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        $"Tổng sản lượng dự kiến ({contractDeliveryBatchDto.TotalPlannedQuantity} kg) vượt quá tổng sản lượng hợp đồng ({contract.TotalQuantity.Value} kg)."
                    );
                }

                // Không cho vượt quá số vòng giao đã khai báo trên hợp đồng
                if (contract.DeliveryRounds.HasValue)
                {
                    // Đếm số đợt giao hiện có (không tính soft-deleted)
                    var currentBatchCount = await _unitOfWork.ContractDeliveryBatchRepository.CountAsync(
                        predicate: b => 
                           !b.IsDeleted && 
                           b.ContractId == contract.ContractId
                    );

                    // Nếu đã đủ số vòng giao -> không cho tạo thêm
                    if (currentBatchCount >= contract.DeliveryRounds.Value)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Hợp đồng đã có đủ {currentBatchCount}/{contract.DeliveryRounds.Value} đợt giao. Không thể tạo thêm."
                        );
                    }

                    // Nếu người dùng nhập số thứ tự đợt giao vượt quá số vòng giao -> chặn
                    if (contractDeliveryBatchDto.DeliveryRound <= 0 ||
                        contractDeliveryBatchDto.DeliveryRound > contract.DeliveryRounds.Value)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Số đợt giao #{contractDeliveryBatchDto.DeliveryRound} không hợp lệ. " +
                            $"Phải nằm trong khoảng 1–{contract.DeliveryRounds.Value}."
                        );
                    }
                }

                // Tổng sản lượng các đợt không vượt quá tổng hợp đồng
                if (contract.TotalQuantity.HasValue)
                {
                    // Tính tổng sản lượng dự kiến của các đợt hiện có
                    var sumPlanned = await _unitOfWork.ContractDeliveryBatchRepository.SumAsync(
                        selector: b => (double?)(b.TotalPlannedQuantity ?? 0),
                        predicate: b => 
                           !b.IsDeleted &&
                           b.ContractId == contract.ContractId
                    ) ?? 0;

                    var newPlanned = contractDeliveryBatchDto.TotalPlannedQuantity;

                    if (sumPlanned + newPlanned > contract.TotalQuantity.Value)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Tổng sản lượng dự kiến của các đợt ({sumPlanned + newPlanned} kg) vượt quá " +
                            $"tổng khối lượng hợp đồng ({contract.TotalQuantity.Value} kg)."
                        );
                    }
                }

                // Tổng khối lượng các dòng đợt giao (items) <= TotalPlannedQuantity của đợt
                if (contractDeliveryBatchDto.ContractDeliveryItems != null
                    && contractDeliveryBatchDto.ContractDeliveryItems.Count > 0)
                {
                    // Nếu Quantity của item là nullable, thay (i.Quantity ?? 0)
                    var itemsTotal = contractDeliveryBatchDto.ContractDeliveryItems
                        .Sum(i => i.PlannedQuantity ?? 0);

                    var batchPlanned = contractDeliveryBatchDto.TotalPlannedQuantity;

                    if (itemsTotal > batchPlanned)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Tổng khối lượng từ các dòng đợt giao ({itemsTotal} kg) " +
                            $"vượt quá sản lượng dự kiến của đợt ({batchPlanned} kg)."
                        );
                    }
                }

                // Cộng dồn theo từng ContractItem: Sum(planned của mọi đợt) + planned mới ≤ Quantity của ContractItem
                if (contractDeliveryBatchDto.ContractDeliveryItems != null
                    && contractDeliveryBatchDto.ContractDeliveryItems.Count > 0)
                {
                    // Lấy map ContractItemId -> MaxQty từ chính hợp đồng (đảm bảo item thuộc hợp đồng)
                    var contractItemMap = contract.ContractItems.ToDictionary(
                        ci => ci.ContractItemId,
                        ci => new {
                            MaxQty = (double?)(ci.Quantity ?? 0),  // đổi sang decimal? nếu bạn dùng decimal
                            CoffeeTypeName = ci.CoffeeType?.TypeName, // nếu có
                            Item = ci
                        }
                    );

                    // Check: tất cả ContractItemId trong DTO phải thuộc hợp đồng
                    var invalidItem = contractDeliveryBatchDto.ContractDeliveryItems
                        .FirstOrDefault(i => !contractItemMap.ContainsKey(i.ContractItemId));

                    if (invalidItem != null)
                    {
                        return new ServiceResult(
                            Const.FAIL_CREATE_CODE,
                            $"Mặt hàng trong đợt giao không thuộc hợp đồng hiện tại (ContractItemId={invalidItem.ContractItemId})."
                        );
                    }

                    // Lấy tổng đã giao (planned) của các đợt KHÁC (đã tồn tại) theo từng ContractItemId
                    var existedSumByItem = await _unitOfWork.ContractDeliveryItemRepository
                        .SumPlannedByContractGroupedAsync(contract.ContractId); // Dictionary<Guid,double>

                    // Tổng planned trong CHÍNH đợt đang tạo, gom theo ContractItemId
                    var newSumByItem = contractDeliveryBatchDto.ContractDeliveryItems
                        .GroupBy(i => i.ContractItemId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(x => (double)(x.PlannedQuantity ?? 0)) // decimal? -> đổi kiểu tương ứng
                        );

                    // So sánh cho từng ContractItem
                    foreach (var kv in newSumByItem)
                    {
                        var contractItemId = kv.Key;
                        var newQty = kv.Value;
                        var existedQty = existedSumByItem.TryGetValue(contractItemId, out var s) ? s : 0;

                        var meta = contractItemMap[contractItemId];
                        var max = meta.MaxQty ?? 0;

                        if (existedQty + newQty > max)
                        {
                            var label = meta.CoffeeTypeName 
                                ?? meta.Item.ContractItemCode 
                                ?? contractItemId.ToString();

                            return new ServiceResult(
                                Const.FAIL_CREATE_CODE,
                                $"Khối lượng giao cho mặt hàng '{label}' ({existedQty + newQty} kg) " +
                                $"vượt quá khối lượng theo dòng hợp đồng ({max} kg)."
                            );
                        }
                    }
                }

                // Sinh mã giao hàng
                string deliveryBatchCode = await _codeGenerator
                    .GenerateDeliveryBatchCodeAsync();

                // Ánh xạ dữ liệu từ DTO vào entity
                var newDeliveryBatch = contractDeliveryBatchDto
                    .MapToNewContractDeliveryBatch(deliveryBatchCode);

                // Lưu vào DB
                await _unitOfWork.ContractDeliveryBatchRepository
                    .CreateAsync(newDeliveryBatch);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu để trả về
                    var createdContractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                        predicate: b => b.DeliveryBatchId == newDeliveryBatch.DeliveryBatchId,
                        include: query => query
                           .Include(b => b.Contract)
                           .Include(b => b.ContractDeliveryItems)
                              .ThenInclude(i => i.ContractItem)
                                 .ThenInclude(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (createdContractDeliveryBatch != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdContractDeliveryBatch.MapToContractDeliveryBatchViewDetailDto();

                        return new ServiceResult(
                            Const.SUCCESS_CREATE_CODE,
                            Const.SUCCESS_CREATE_MSG,
                            responseDto
                        );
                    }

                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Tạo thành công nhưng không truy xuất được dữ liệu để trả về."
                    );
                }

                return new ServiceResult(
                    Const.FAIL_CREATE_CODE,
                    Const.FAIL_CREATE_MSG
                );
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ nếu có lỗi xảy ra trong quá trình
                return new ServiceResult(
                    Const.ERROR_EXCEPTION, 
                    ex.Message
                );
            }
        }

        public async Task<IServiceResult> Update(ContractDeliveryBatchUpdateDto contractDeliveryBatchDto, Guid userId)
        {
            try
            {
                Guid? managerId = null;

                // Xác định Manager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s => 
                           s.UserId == userId && 
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                    {
                        managerId = staff.SupervisorId;
                    }
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE, 
                        "Không có quyền cập nhật đợt giao hàng."
                    );
                }

                // Lấy đợt giao hàng cần cập nhật
                var deliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: b => 
                       b.DeliveryBatchId == contractDeliveryBatchDto.DeliveryBatchId && 
                       !b.IsDeleted,
                    include: query => query
                        .Include(b => b.Contract)
                        .Include(b => b.ContractDeliveryItems),
                    asNoTracking: false
                );

                if (deliveryBatch == null || 
                    deliveryBatch.Contract?.SellerId != managerId)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE, 
                        "Không tìm thấy đợt giao hàng hoặc không thuộc quyền quản lý."
                    );
                }

                var contract = deliveryBatch.Contract;

                // Kiểm tra ngày và sản lượng
                if (contractDeliveryBatchDto.ExpectedDeliveryDate < contract.StartDate || 
                    contractDeliveryBatchDto.ExpectedDeliveryDate > contract.EndDate)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Ngày giao dự kiến phải nằm trong thời gian hợp đồng: {contract.StartDate:dd/MM/yyyy} - {contract.EndDate:dd/MM/yyyy}."
                    );
                }

                if (contract.TotalQuantity.HasValue && 
                    contractDeliveryBatchDto.TotalPlannedQuantity > contract.TotalQuantity.Value)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        $"Tổng sản lượng dự kiến ({contractDeliveryBatchDto.TotalPlannedQuantity} kg) vượt quá hợp đồng ({contract.TotalQuantity.Value} kg)."
                    );
                }

                // Kiểm tra DeliveryRound (nếu thay đổi và bị trùng)
                if (contractDeliveryBatchDto.DeliveryRound != deliveryBatch.DeliveryRound)
                {
                    bool isDuplicate = await _unitOfWork.ContractDeliveryBatchRepository.AnyAsync(
                        predicate: b =>
                            !b.IsDeleted &&
                            b.ContractId == contract.ContractId &&
                            b.DeliveryRound == contractDeliveryBatchDto.DeliveryRound &&
                            b.DeliveryBatchId != contractDeliveryBatchDto.DeliveryBatchId
                    );

                    if (isDuplicate)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Đợt giao hàng số {contractDeliveryBatchDto.DeliveryRound} đã tồn tại trong hợp đồng."
                        );
                    }
                }

                // Không cho vượt quá số vòng giao đã khai báo trên hợp đồng
                if (contract.DeliveryRounds.HasValue)
                {
                    // Đếm số đợt giao hiện có (không tính soft-deleted và không tính đợt đang update)
                    var currentBatchCount = await _unitOfWork.ContractDeliveryBatchRepository.CountAsync(
                        predicate: b => 
                           !b.IsDeleted && 
                           b.ContractId == contract.ContractId &&
                           b.DeliveryBatchId != contractDeliveryBatchDto.DeliveryBatchId
                    );

                    // Nếu đã đủ số vòng giao -> không cho tạo thêm
                    if (currentBatchCount >= contract.DeliveryRounds.Value)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Hợp đồng đã có đủ {currentBatchCount}/{contract.DeliveryRounds.Value} đợt giao. Không thể tạo thêm."
                        );
                    }

                    // Nếu người dùng nhập số thứ tự đợt giao vượt quá số vòng giao -> chặn
                    if (contractDeliveryBatchDto.DeliveryRound <= 0 ||
                        contractDeliveryBatchDto.DeliveryRound > contract.DeliveryRounds.Value)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Số đợt giao #{contractDeliveryBatchDto.DeliveryRound} không hợp lệ. " +
                            $"Phải nằm trong khoảng 1–{contract.DeliveryRounds.Value}."
                        );
                    }
                }

                // Tổng sản lượng các đợt không vượt quá tổng hợp đồng
                if (contract.TotalQuantity.HasValue)
                {
                    // Tính tổng sản lượng dự kiến của các đợt hiện có (không tính đợt đang update)
                    var sumPlanned = await _unitOfWork.ContractDeliveryBatchRepository.SumAsync(
                        selector: b => (double?)(b.TotalPlannedQuantity ?? 0),
                        predicate: b => 
                           !b.IsDeleted &&
                           b.ContractId == contract.ContractId &&
                           b.DeliveryBatchId != contractDeliveryBatchDto.DeliveryBatchId
                    ) ?? 0;

                    var newPlanned = contractDeliveryBatchDto.TotalPlannedQuantity;

                    if (sumPlanned + newPlanned > contract.TotalQuantity.Value)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Tổng sản lượng dự kiến của các đợt ({sumPlanned + newPlanned} kg) vượt quá " +
                            $"tổng khối lượng hợp đồng ({contract.TotalQuantity.Value} kg)."
                        );
                    }
                }

                // Tổng khối lượng các dòng đợt giao (items) <= TotalPlannedQuantity của đợt
                if (contractDeliveryBatchDto.ContractDeliveryItems != null
                    && contractDeliveryBatchDto.ContractDeliveryItems.Count > 0)
                {
                    // Nếu Quantity của item là nullable, thay (i.Quantity ?? 0)
                    var itemsTotal = contractDeliveryBatchDto.ContractDeliveryItems
                        .Sum(i => i.PlannedQuantity ?? 0);

                    var batchPlanned = contractDeliveryBatchDto.TotalPlannedQuantity;

                    if (itemsTotal > batchPlanned)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Tổng khối lượng từ các dòng đợt giao ({itemsTotal} kg) " +
                            $"vượt quá sản lượng dự kiến của đợt ({batchPlanned} kg)."
                        );
                    }
                }

                // Cộng dồn theo từng ContractItem: Sum(planned của mọi đợt) + planned mới ≤ Quantity của ContractItem
                if (contractDeliveryBatchDto.ContractDeliveryItems != null
                    && contractDeliveryBatchDto.ContractDeliveryItems.Count > 0)
                {
                    // Lấy map ContractItemId -> MaxQty từ chính hợp đồng (đảm bảo item thuộc hợp đồng)
                    var contractItems = await _unitOfWork.ContractRepository.GetByIdAsync(
                        predicate: c => 
                           c.ContractId == contract.ContractId && 
                           !c.IsDeleted,
                        include: q => q
                           .Include(c => c.ContractItems.Where(ci => !ci.IsDeleted))
                              .ThenInclude(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (contractItems == null)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            "Không thể truy xuất thông tin hợp đồng để kiểm tra."
                        );
                    }

                    var contractItemMap = contractItems.ContractItems.ToDictionary(
                        ci => ci.ContractItemId,
                        ci => new {
                            MaxQty = (double?)(ci.Quantity ?? 0),
                            CoffeeTypeName = ci.CoffeeType?.TypeName,
                            Item = ci
                        }
                    );

                    // Check: tất cả ContractItemId trong DTO phải thuộc hợp đồng
                    var invalidItem = contractDeliveryBatchDto.ContractDeliveryItems
                        .FirstOrDefault(i => !contractItemMap.ContainsKey(i.ContractItemId));

                    if (invalidItem != null)
                    {
                        return new ServiceResult(
                            Const.FAIL_UPDATE_CODE,
                            $"Mặt hàng trong đợt giao không thuộc hợp đồng hiện tại (ContractItemId={invalidItem.ContractItemId})."
                        );
                    }

                    // Lấy tổng đã giao (planned) của các đợt KHÁC (đã tồn tại) theo từng ContractItemId
                    var existedSumByItem = await _unitOfWork.ContractDeliveryItemRepository
                        .SumPlannedByContractGroupedAsync(contract.ContractId);

                    // Tổng planned trong CHÍNH đợt đang tạo, gom theo ContractItemId
                    var newSumByItem = contractDeliveryBatchDto.ContractDeliveryItems
                        .GroupBy(i => i.ContractItemId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(x => (double)(x.PlannedQuantity ?? 0)) // decimal? -> đổi kiểu tương ứng
                        );

                    // Tổng planned trong CHÍNH đợt đang update, gom theo ContractItemId
                    var currentBatchSumByItem = deliveryBatch.ContractDeliveryItems
                        .Where(i => !i.IsDeleted)
                        .GroupBy(i => i.ContractItemId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(x => (double)(x.PlannedQuantity))
                        );

                    // So sánh cho từng ContractItem
                    foreach (var kv in newSumByItem)
                    {
                        var contractItemId = kv.Key;
                        var newQty = kv.Value;

                        var currentQty = currentBatchSumByItem.TryGetValue(contractItemId, out var c) ? c : 0;
                        var existedQty = existedSumByItem.TryGetValue(contractItemId, out var s) ? s : 0;

                        var meta = contractItemMap[contractItemId];
                        var max = meta.MaxQty ?? 0;

                        // Tổng tất cả - Đợt hiện tại + Đợt mới
                        var totalQty = existedQty - currentQty + newQty;

                        if (totalQty > max)
                        {
                            var label = meta.CoffeeTypeName 
                                ?? meta.Item.ContractItemCode 
                                ?? contractItemId.ToString();

                            return new ServiceResult(
                                Const.FAIL_UPDATE_CODE,
                                $"Khối lượng giao cho mặt hàng '{label}' ({totalQty} kg) " +
                                $"vượt quá khối lượng theo dòng hợp đồng ({max} kg)."
                            );
                        }
                    }
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                contractDeliveryBatchDto.MapToUpdatedContractDeliveryBatch(deliveryBatch);

                // Đồng bộ ContractDeliveryItems
                var dtoItemIds = contractDeliveryBatchDto.ContractDeliveryItems
                    .Select(i => i.DeliveryItemId).ToHashSet();

                var now = DateHelper.NowVietnamTime();

                foreach (var oldItem in deliveryBatch.ContractDeliveryItems)
                {
                    if (!dtoItemIds.Contains(oldItem.DeliveryItemId))
                    {
                        oldItem.IsDeleted = true;
                        oldItem.UpdatedAt = now;

                        await _unitOfWork.ContractDeliveryItemRepository
                            .UpdateAsync(oldItem);
                    }
                }

                foreach (var itemDto in contractDeliveryBatchDto.ContractDeliveryItems)
                {
                    // Pattern giống ContractService - kiểm tra Guid.Empty
                    if (itemDto.DeliveryItemId != Guid.Empty)
                    {
                        var existingItem = deliveryBatch.ContractDeliveryItems
                            .FirstOrDefault(i => i.DeliveryItemId == itemDto.DeliveryItemId && !i.IsDeleted);

                        if (existingItem != null)
                        {
                            // Cập nhật item hiện có
                            existingItem.ContractItemId = itemDto.ContractItemId;
                            existingItem.PlannedQuantity = itemDto.PlannedQuantity ?? 0;
                            existingItem.FulfilledQuantity = itemDto.FulfilledQuantity;
                            existingItem.Note = itemDto.Note;
                            existingItem.IsDeleted = false;
                            existingItem.UpdatedAt = now;

                            await _unitOfWork.ContractDeliveryItemRepository
                                .UpdateAsync(existingItem);
                        }
                    }
                    else
                    {
                        var newItem = new ContractDeliveryItem
                        {
                            DeliveryItemId = Guid.NewGuid(),
                            DeliveryItemCode = $"DLI-{(contractDeliveryBatchDto.ContractDeliveryItems.Count + 1):D3}-{deliveryBatch.DeliveryBatchCode}",
                            DeliveryBatchId = deliveryBatch.DeliveryBatchId,
                            ContractItemId = itemDto.ContractItemId,
                            PlannedQuantity = itemDto.PlannedQuantity ?? 0,
                            FulfilledQuantity = itemDto.FulfilledQuantity,
                            Note = itemDto.Note,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsDeleted = false
                        };

                        await _unitOfWork.ContractDeliveryItemRepository
                            .CreateAsync(newItem);
                    }
                }

                // Cập nhật contractDeliveryBatch ở repository
                await _unitOfWork.ContractDeliveryBatchRepository
                    .UpdateAsync(deliveryBatch);

                // Lưu thay đổi vào database
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại contract sau update để trả DTO
                    var updatedBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                        predicate: b => 
                           b.DeliveryBatchId == deliveryBatch.DeliveryBatchId &&
                           !b.IsDeleted,
                        include: query => query
                           .Include(b => b.Contract)
                           .Include(b => b.ContractDeliveryItems.Where(i => !i.IsDeleted))
                              .ThenInclude(i => i.ContractItem)
                                 .ThenInclude(ci => ci.CoffeeType),
                        asNoTracking: true
                    );

                    if (updatedBatch != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedBatch.MapToContractDeliveryBatchViewDetailDto();

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

        public async Task<IServiceResult> DeleteContractDeliveryBatchById(Guid deliveryBatchId, Guid userId)
        {
            try
            {
                // Lấy ManagerId từ userId
                Guid? managerId = null;

                // Ưu tiên kiểm tra BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s => 
                           s.UserId == userId && 
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                        managerId = staff.SupervisorId;
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không xác định được Manager hoặc Supervisor từ userId."
                    );
                }

                // Tìm ContractDeliveryBatch theo ID kèm danh sách ContractDeliveryItems
                var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: cdb => 
                       cdb.DeliveryBatchId == deliveryBatchId && 
                       cdb.Contract != null &&
                       cdb.Contract.SellerId == managerId,
                    include: query => query
                       .Include(cdb => cdb.ContractDeliveryItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractDeliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Xóa từng ContractDeliveryItem trước (nếu có)
                    if (contractDeliveryBatch.ContractDeliveryItems != null && 
                        contractDeliveryBatch.ContractDeliveryItems.Any())
                    {
                        foreach (var item in contractDeliveryBatch.ContractDeliveryItems)
                        {
                            // Xóa ContractDeliveryItem khỏi repository
                            await _unitOfWork.ContractDeliveryItemRepository
                                .RemoveAsync(item);
                        }
                    }

                    // Xóa ContractDeliveryBatch khỏi repository
                    await _unitOfWork.ContractDeliveryBatchRepository
                        .RemoveAsync(contractDeliveryBatch);

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

        public async Task<IServiceResult> SoftDeleteContractDeliveryBatchById(Guid deliveryBatchId, Guid userId)
        {
            try
            {
                // Lấy ManagerId từ userId
                Guid? managerId = null;

                // Ưu tiên kiểm tra BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager != null)
                {
                    managerId = manager.ManagerId;
                }
                else
                {
                    // Nếu không phải Manager, kiểm tra BusinessStaff
                    var staff = await _unitOfWork.BusinessStaffRepository.GetByIdAsync(
                        predicate: s =>
                           s.UserId == userId &&
                           !s.IsDeleted,
                        asNoTracking: true
                    );

                    if (staff != null)
                        managerId = staff.SupervisorId;
                }

                if (managerId == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không xác định được Manager hoặc Supervisor từ userId."
                    );
                }

                // Tìm contractDeliveryBatch theo ID
                var contractDeliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: cdb => 
                       cdb.DeliveryBatchId == deliveryBatchId &&
                       !cdb.IsDeleted &&
                       cdb.Contract != null &&
                       cdb.Contract.SellerId == managerId,
                    include: query => query
                       .Include(cdb => cdb.ContractDeliveryItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (contractDeliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Đánh dấu contractDeliveryBatch là đã xóa
                    contractDeliveryBatch.IsDeleted = true;
                    contractDeliveryBatch.UpdatedAt = DateHelper.NowVietnamTime();

                    // Đánh dấu tất cả ContractDeliveryItems là đã xóa
                    foreach (var item in contractDeliveryBatch.ContractDeliveryItems.Where(i => !i.IsDeleted))
                    {
                        item.IsDeleted = true;
                        item.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của item
                        await _unitOfWork.ContractDeliveryItemRepository
                            .UpdateAsync(item);
                    }

                    // Cập nhật xoá mềm contractDeliveryBatch ở repository
                    await _unitOfWork.ContractDeliveryBatchRepository
                        .UpdateAsync(contractDeliveryBatch);

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
