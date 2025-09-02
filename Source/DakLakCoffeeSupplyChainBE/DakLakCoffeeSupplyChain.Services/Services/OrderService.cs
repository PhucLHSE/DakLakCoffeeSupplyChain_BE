using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICodeGenerator _codeGenerator;

        public OrderService(IUnitOfWork unitOfWork, ICodeGenerator codeGenerator)
        {
            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));

            _codeGenerator = codeGenerator
                ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        public async Task<IServiceResult> GetAll(Guid userId)
        {
            Guid? managerId = null;

            // Kiểm tra BusinessManager
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

            // Truy vấn Order thuộc các DeliveryBatch trong hợp đồng của Manager hiện tại
            var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                predicate: o =>
                    !o.IsDeleted &&
                    o.DeliveryBatch != null &&
                    o.DeliveryBatch.Contract != null &&
                    o.DeliveryBatch.Contract.SellerId == managerId,
                include: query => query
                    .Include(o => o.DeliveryBatch)
                        .ThenInclude(db => db.Contract),
                orderBy: query => query.OrderBy(o => o.OrderDate),
                asNoTracking: true
            );

            // Kiểm tra nếu không có dữ liệu
            if (orders == null || 
                !orders.Any())
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new List<OrderViewAllDto>()   // Trả về danh sách rỗng
                );
            }
            else
            {
                // Map danh sách entity sang DTO
                var orderDtos = orders
                    .Select(order => order.MapToOrderViewAllDto())
                    .ToList();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    orderDtos
                );
            }
        }

        public async Task<IServiceResult> GetById(Guid orderId, Guid userId)
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
                    Const.WARNING_NO_DATA_CODE,
                    "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                );
            }

            // Tìm order theo ID
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                predicate: o =>
                   o.OrderId == orderId &&
                   !o.IsDeleted &&
                   o.DeliveryBatch != null &&
                   o.DeliveryBatch.Contract != null &&
                   o.DeliveryBatch.Contract.SellerId == managerId,
                include: query => query
                   .Include(o => o.DeliveryBatch)
                      .ThenInclude(db => db.Contract)
                   .Include(o => o.OrderItems.Where(oi => !oi.IsDeleted))
                      .ThenInclude(oi => oi.Product)
                   .Include(o => o.OrderItems.Where(oi => !oi.IsDeleted))
                      .ThenInclude(oi => oi.ContractDeliveryItem),
                asNoTracking: true
            );

            // Kiểm tra nếu không tìm thấy Order
            if (order == null)
            {
                return new ServiceResult(
                    Const.WARNING_NO_DATA_CODE,
                    Const.WARNING_NO_DATA_MSG,
                    new OrderViewDetailsDto()  // Trả về DTO rỗng
                );
            }
            else
            {
                // Sắp xếp danh sách OrderItems theo CreatedAt tăng dần
                order.OrderItems = order.OrderItems
                    .OrderBy(oi => oi.CreatedAt)
                    .ToList();

                // Map sang DTO chi tiết để trả về
                var orderDto = order.MapToOrderViewDetailsDto();

                return new ServiceResult(
                    Const.SUCCESS_READ_CODE,
                    Const.SUCCESS_READ_MSG,
                    orderDto
                );
            }
        }

        public async Task<IServiceResult> Create(OrderCreateDto orderCreateDto, Guid userId)
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
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                    );
                }

                // Kiểm tra DeliveryBatch tồn tại & quyền
                var deliveryBatch = await _unitOfWork.ContractDeliveryBatchRepository.GetByIdAsync(
                    predicate: b => 
                       b.DeliveryBatchId == orderCreateDto.DeliveryBatchId && 
                       !b.IsDeleted,
                    include: query => query
                       .Include(b => b.Contract),
                    asNoTracking: true
                );

                if (deliveryBatch == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy lô giao hàng hoặc lô đã bị xoá."
                    );
                }

                if (deliveryBatch.Contract.SellerId != managerId)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Bạn không có quyền tạo đơn hàng cho lô giao hàng này."
                    );
                }

                // Một lô chỉ được tạo 1 Order (nếu đây là rule)
                var isOrderExists = await _unitOfWork.OrderRepository.AnyAsync(
                    predicate: o => 
                       o.DeliveryBatchId == orderCreateDto.DeliveryBatchId && 
                       o.IsDeleted == false
                );

                if (isOrderExists)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Lô giao hàng này đã có đơn hàng được tạo trước đó."
                    );
                }

                // Gom ID duy nhất
                var cdiIds = orderCreateDto.OrderItems
                    .Select(i => i.ContractDeliveryItemId).Distinct().ToList();

                var productIds = orderCreateDto.OrderItems
                    .Select(i => i.ProductId).Distinct().ToList();

                // Lấy CDI kèm ContractItem và CoffeeType
                var cdis = await _unitOfWork.ContractDeliveryItemRepository.GetAllAsync(
                    predicate: cdi => 
                       cdiIds.Contains(cdi.DeliveryItemId) && 
                       !cdi.IsDeleted,
                    include: q => q
                       .Include(x => x.ContractItem)
                          .ThenInclude(ci => ci.CoffeeType),
                    asNoTracking: true
                );

                var cdiById = cdis.ToDictionary(x => x.DeliveryItemId);

                // Lấy Product
                var products = await _unitOfWork.ProductRepository.GetAllAsync(
                    predicate: p => 
                       productIds.Contains(p.ProductId) && 
                       !p.IsDeleted,
                    include: q => q
                       .Include(p => p.CoffeeType),
                    asNoTracking: true
                );

                var productById = products.ToDictionary(p => p.ProductId);

                var errors = new List<string>();

                // Kiểm tra tồn tại, đúng lô, CoffeeType, và từng dòng không vượt khả dụng
                foreach (var item in orderCreateDto.OrderItems)
                {
                    if (!cdiById.TryGetValue(item.ContractDeliveryItemId, out var cdi))
                    {
                        errors.Add($"Không tìm thấy mục giao hàng (CDI) cho dòng đơn hàng đã chọn.");

                        continue;
                    }

                    if (cdi.DeliveryBatchId != orderCreateDto.DeliveryBatchId)
                    {
                        var cdiCoffeeTypeName = cdi.ContractItem?.CoffeeType?.TypeName
                                ?? cdi.ContractItem?.CoffeeTypeId.ToString()
                                ?? "không rõ loại";

                        errors.Add($"Mục giao hàng ({cdiCoffeeTypeName}) không thuộc lô giao hàng đang tạo đơn.");

                        continue;
                    }

                    if (cdi.ContractItem == null)
                    {
                        errors.Add("Mục giao hàng này không gắn ContractItem hợp lệ.");

                        continue;
                    }

                    if (!productById.TryGetValue(item.ProductId, out var p))
                    {
                        errors.Add("Không tìm thấy sản phẩm tương ứng cho dòng đơn hàng.");

                        continue;
                    }

                    // CoffeeType phải trùng giữa ContractItem và Product
                    var productLabel = p.ProductName ?? $"Product #{p.ProductId}";

                    var productCoffeeTypeName = p.CoffeeType?.TypeName
                                                ?? p.CoffeeTypeId.ToString()
                                                ?? "không rõ loại";

                    var contractCoffeeTypeName = cdi.ContractItem?.CoffeeType?.TypeName
                                                 ?? cdi.ContractItem?.CoffeeTypeId.ToString()
                                                 ?? "không rõ loại";

                    if (cdi.ContractItem?.CoffeeTypeId != p.CoffeeTypeId)
                    {
                        errors.Add(
                            $"Loại cà phê không khớp: {productLabel} ({productCoffeeTypeName}) " +
                            $"≠ ContractItem ({contractCoffeeTypeName})."
                        );
                    }

                    // Số lượng khả dụng (điều chỉnh tên field cho đúng schema của bạn)
                    double cdiAvailable = Math.Max(0d, cdi.PlannedQuantity - (cdi.FulfilledQuantity ?? 0d));

                    double productAvailable = p.QuantityAvailable ?? 0;

                    double requested = item.Quantity ?? 0;

                    if (requested <= 0)
                        errors.Add($"Số lượng cho {productLabel} phải lớn hơn 0.");

                    if (requested > cdiAvailable)
                        errors.Add(
                            $"Số lượng cho {productLabel} vượt mức cho phép của mục giao hàng ({contractCoffeeTypeName}): " +
                            $"{requested} > {cdiAvailable}."
                        );

                    if (requested > productAvailable)
                    {
                        errors.Add(
                            $"Số lượng cho {productLabel} vượt tồn kho khả dụng: {requested} > {productAvailable}."
                        );
                    }
                }

                // Tổng theo CDI
                var totalByCdi = orderCreateDto.OrderItems
                    .GroupBy(i => i.ContractDeliveryItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

                foreach (var kv in totalByCdi)
                {
                    if (cdiById.TryGetValue(kv.Key, out var cdi))
                    {
                        var contractCoffeeTypeName = cdi.ContractItem?.CoffeeType?.TypeName
                                     ?? cdi.ContractItem?.CoffeeTypeId.ToString()
                                     ?? "không rõ loại";

                        double cdiAvailable = Math.Max(0d, cdi.PlannedQuantity - (cdi.FulfilledQuantity ?? 0d));

                        if (kv.Value > cdiAvailable)
                            errors.Add(
                                $"Tổng số lượng cho mục giao hàng ({contractCoffeeTypeName}) vượt mức: " +
                                $"{kv.Value} > {cdiAvailable}."
                            );
                    }
                }

                // Tổng theo Product
                var totalByProduct = orderCreateDto.OrderItems
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

                foreach (var kv in totalByProduct)
                {
                    if (productById.TryGetValue(kv.Key, out var p))
                    {
                        var productLabel = p.ProductName ?? $"Product #{p.ProductId}";

                        double productAvailable = p.QuantityAvailable ?? 0;

                        if (kv.Value > productAvailable)
                            errors.Add(
                                $"Tổng số lượng cho {productLabel} vượt tồn kho khả dụng: " +
                                $"{kv.Value} > {productAvailable}."
                            );
                    }
                }

                if (errors.Count > 0)
                {
                    return new ServiceResult(
                        Const.FAIL_CREATE_CODE,
                        "Order validation failed:\n- " + string.Join("\n- ", errors)
                    );
                }

                // Cập nhật QuantityAvailable của Products (trừ số lượng khi đặt hàng)
                await UpdateProductQuantityAvailable(orderCreateDto.OrderItems, true);

                // Sinh mã đơn hàng
                string orderCode = await _codeGenerator.GenerateOrderCodeAsync();

                // (Tuỳ chọn) Chuẩn hoá default status nếu cần
                // if (orderCreateDto.Status == default) orderCreateDto.Status = OrderStatus.Processing;

                // Ánh xạ dữ liệu từ DTO vào entity
                var newOrder = orderCreateDto
                    .MapToNewOrder(orderCode, userId);

                // Tính tổng tiền và gán lại
                newOrder.TotalAmount = newOrder.OrderItems
                    .Where(i => !i.IsDeleted)
                    .Sum(i => i.TotalPrice);

                // Lưu vào DB
                await _unitOfWork.OrderRepository
                    .CreateAsync(newOrder);

                // Lưu thay đổi
                var result = await _unitOfWork.SaveChangesAsync();

                if (result > 0)
                {
                    // Truy xuất lại dữ liệu để trả về
                    var createdOrder = await _unitOfWork.OrderRepository.GetByIdAsync(
                        predicate: o => o.OrderId == newOrder.OrderId,
                        include: query => query
                            .Include(o => o.DeliveryBatch)
                               .ThenInclude(b => b.Contract)
                            .Include(o => o.OrderItems)
                               .ThenInclude(i => i.Product),
                        asNoTracking: true
                    );

                    if (createdOrder != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = createdOrder.MapToOrderViewDetailsDto();

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

        public async Task<IServiceResult> Update(OrderUpdateDto orderUpdateDto, Guid userId)
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
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy Manager hoặc Staff tương ứng với tài khoản."
                    );
                }

                // Lấy đơn hàng cần cập nhật
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o => 
                       o.OrderId == orderUpdateDto.OrderId && 
                       !o.IsDeleted &&
                       o.DeliveryBatch != null &&
                       o.DeliveryBatch.Contract != null &&
                       o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                        .Include(o => o.OrderItems)
                        .Include(o => o.DeliveryBatch)
                           .ThenInclude(b => b.Contract),
                    asNoTracking: false
                );

                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập."
                    );
                }

                // Dùng DeliveryBatchId hiện tại của Order
                var deliveryBatchId = order.DeliveryBatchId;

                // Gom ID duy nhất
                var cdiIds = orderUpdateDto.OrderItems
                    .Select(i => i.ContractDeliveryItemId)
                    .Distinct()
                    .ToList();

                var productIds = orderUpdateDto.OrderItems
                    .Select(i => i.ProductId)
                    .Distinct()
                    .ToList();

                // Lấy CDI kèm ContractItem và CoffeeType
                var cdis = await _unitOfWork.ContractDeliveryItemRepository.GetAllAsync(
                    predicate: cdi => 
                       cdiIds.Contains(cdi.DeliveryItemId) && 
                       !cdi.IsDeleted,
                    include: q => q
                        .Include(x => x.ContractItem)
                            .ThenInclude(ci => ci.CoffeeType),
                    asNoTracking: true
                );

                var cdiById = cdis.ToDictionary(x => x.DeliveryItemId);

                // Lấy Product kèm CoffeeType
                var products = await _unitOfWork.ProductRepository.GetAllAsync(
                    predicate: p => 
                       productIds.Contains(p.ProductId) && 
                       !p.IsDeleted,
                    include: q => q
                       .Include(p => p.CoffeeType),
                    asNoTracking: true
                );

                var productById = products.ToDictionary(p => p.ProductId);

                var errors = new List<string>();

                // Kiểm tra từng dòng
                foreach (var item in orderUpdateDto.OrderItems)
                {
                    if (!cdiById.TryGetValue(item.ContractDeliveryItemId, out var cdi))
                    {
                        errors.Add("Không tìm thấy mục giao hàng (CDI) cho dòng đơn hàng đã chọn.");

                        continue;
                    }

                    if (cdi.DeliveryBatchId != deliveryBatchId)
                    {
                        var cdiCoffeeTypeName = cdi.ContractItem?.CoffeeType?.TypeName
                                                ?? cdi.ContractItem?.CoffeeTypeId.ToString()
                                                ?? "không rõ loại";

                        errors.Add($"Mục giao hàng ({cdiCoffeeTypeName}) không thuộc lô giao hàng của đơn hiện tại.");

                        continue;
                    }

                    if (cdi.ContractItem == null)
                    {
                        errors.Add("Mục giao hàng này không gắn ContractItem hợp lệ.");

                        continue;
                    }

                    if (!productById.TryGetValue(item.ProductId, out var p))
                    {
                        errors.Add("Không tìm thấy sản phẩm tương ứng cho dòng đơn hàng.");

                        continue;
                    }

                    // CoffeeType phải trùng giữa ContractItem và Product
                    var productLabel = p.ProductName ?? $"Product #{p.ProductId}";

                    var productCoffeeTypeName = p.CoffeeType?.TypeName ?? p.CoffeeTypeId.ToString() ?? "không rõ loại";

                    var contractCoffeeTypeName = cdi.ContractItem?.CoffeeType?.TypeName
                                                 ?? cdi.ContractItem?.CoffeeTypeId.ToString()
                                                 ?? "không rõ loại";

                    if (cdi.ContractItem?.CoffeeTypeId != p.CoffeeTypeId)
                    {
                        errors.Add($"Loại cà phê không khớp: {productLabel} ({productCoffeeTypeName}) ≠ ContractItem ({contractCoffeeTypeName}).");
                    }

                    // Số lượng khả dụng
                    double requested = item.Quantity ?? 0;

                    double cdiAvailable = Math.Max(0d, cdi.PlannedQuantity - (cdi.FulfilledQuantity ?? 0d));

                    double productAvailable = p.QuantityAvailable ?? 0d;

                    if (requested <= 0)
                        errors.Add($"Số lượng cho {productLabel} phải lớn hơn 0.");

                    if (requested > cdiAvailable)
                        errors.Add($"Số lượng cho {productLabel} vượt mức mục giao hàng ({contractCoffeeTypeName}): {requested} > {cdiAvailable}.");

                    if (requested > productAvailable)
                        errors.Add($"Số lượng cho {productLabel} vượt tồn kho khả dụng: {requested} > {productAvailable}.");
                }

                // Tổng theo CDI
                var totalByCdi = orderUpdateDto.OrderItems
                    .GroupBy(i => i.ContractDeliveryItemId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

                foreach (var kv in totalByCdi)
                {
                    if (cdiById.TryGetValue(kv.Key, out var cdi))
                    {
                        var contractCoffeeTypeName = cdi.ContractItem?.CoffeeType?.TypeName
                                                     ?? cdi.ContractItem?.CoffeeTypeId.ToString()
                                                     ?? "không rõ loại";

                        double cdiAvailable = Math.Max(0d, cdi.PlannedQuantity - (cdi.FulfilledQuantity ?? 0d));

                        if (kv.Value > cdiAvailable)
                            errors.Add($"Tổng số lượng cho mục giao hàng ({contractCoffeeTypeName}) vượt mức: {kv.Value} > {cdiAvailable}.");
                    }
                }

                // Tổng theo Product
                var totalByProduct = orderUpdateDto.OrderItems
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0));

                foreach (var kv in totalByProduct)
                {
                    if (productById.TryGetValue(kv.Key, out var p))
                    {
                        var productLabel = p.ProductName ?? $"Product #{p.ProductId}";

                        double productAvailable = p.QuantityAvailable ?? 0d;

                        if (kv.Value > productAvailable)
                            errors.Add($"Tổng số lượng cho {productLabel} vượt tồn kho khả dụng: {kv.Value} > {productAvailable}.");
                    }
                }

                if (errors.Count > 0)
                {
                    return new ServiceResult(
                        Const.FAIL_UPDATE_CODE,
                        "Order validation failed:\n- " + string.Join("\n- ", errors)
                    );
                }

                // Ánh xạ dữ liệu từ DTO vào entity
                orderUpdateDto.MapToUpdatedOrder(order);

                // Tập hợp các ID gửi từ client
                var dtoItemIds = orderUpdateDto.OrderItems
                    .Where(i => i.OrderItemId != Guid.Empty)
                    .Select(i => i.OrderItemId)
                    .ToHashSet();

                var now = DateHelper.NowVietnamTime();

                // Cộng lại số lượng của những item cũ sẽ bị xóa
                var itemsToDelete = order.OrderItems
                    .Where(i => !i.IsDeleted && !dtoItemIds.Contains(i.OrderItemId))
                    .Select(oi => new OrderItemUpdateDto
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity
                    });

                await UpdateProductQuantityAvailable(itemsToDelete, false);

                // Xoá mềm những item cũ không còn trong DTO
                foreach (var oldItem in order.OrderItems.Where(i => !i.IsDeleted))
                {
                    if (!dtoItemIds.Contains(oldItem.OrderItemId))
                    {
                        oldItem.IsDeleted = true;
                        oldItem.UpdatedAt = now;

                        await _unitOfWork.OrderItemRepository
                            .UpdateAsync(oldItem);
                    }
                }

                // Lưu trữ thông tin OrderItems cũ để tính chênh lệch
                var oldItemQuantities = order.OrderItems
                    .Where(i => !i.IsDeleted)
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                // Xử lý thêm/cập nhật các item
                foreach (var itemDto in orderUpdateDto.OrderItems)
                {
                    OrderItem? existingItem = null;

                    if (itemDto.OrderItemId != Guid.Empty)
                    {
                        existingItem = order.OrderItems
                            .FirstOrDefault(i => i.OrderItemId == itemDto.OrderItemId);
                    }

                    if (existingItem == null)
                    {
                        existingItem = order.OrderItems
                            .FirstOrDefault(i =>
                                i.ProductId == itemDto.ProductId &&
                                i.ContractDeliveryItemId == itemDto.ContractDeliveryItemId &&
                                !i.IsDeleted);
                    }

                    if (existingItem != null)
                    {
                        // Cập nhật
                        existingItem.Quantity = itemDto.Quantity ?? 0;
                        existingItem.UnitPrice = itemDto.UnitPrice ?? 0;
                        existingItem.DiscountAmount = itemDto.DiscountAmount ?? 0;
                        existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice - existingItem.DiscountAmount;
                        existingItem.Note = itemDto.Note;
                        existingItem.IsDeleted = false;
                        existingItem.UpdatedAt = now;

                        await _unitOfWork.OrderItemRepository
                            .UpdateAsync(existingItem);
                    }
                    else
                    {
                        // Tạo mới
                        var newItem = new OrderItem
                        {
                            OrderItemId = Guid.NewGuid(),
                            OrderId = order.OrderId,
                            ProductId = itemDto.ProductId,
                            ContractDeliveryItemId = itemDto.ContractDeliveryItemId,
                            Quantity = itemDto.Quantity ?? 0,
                            UnitPrice = itemDto.UnitPrice ?? 0,
                            DiscountAmount = itemDto.DiscountAmount ?? 0,
                            TotalPrice = (itemDto.Quantity ?? 0) * (itemDto.UnitPrice ?? 0) - (itemDto.DiscountAmount ?? 0),
                            Note = itemDto.Note,
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsDeleted = false
                        };

                        await _unitOfWork.OrderItemRepository
                            .CreateAsync(newItem);

                        order.OrderItems.Add(newItem);
                    }
                }

                // Tính chênh lệch và cập nhật QuantityAvailable
                var newItemQuantities = orderUpdateDto.OrderItems
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity ?? 0));

                var quantityDifferences = new List<OrderItemUpdateDto>();

                // Tính chênh lệch cho từng Product
                foreach (var productId in oldItemQuantities.Keys.Union(newItemQuantities.Keys))
                {
                    var oldQuantity = oldItemQuantities.GetValueOrDefault(productId, 0);
                    var newQuantity = newItemQuantities.GetValueOrDefault(productId, 0);
                    var difference = newQuantity - oldQuantity;

                    if (difference != 0)
                    {
                        var absDifference = Math.Abs(difference ?? 0);

                        quantityDifferences.Add(new OrderItemUpdateDto
                        {
                            ProductId = productId,
                            Quantity = absDifference
                        });

                        // Nếu difference > 0: tăng đặt hàng, cần trừ thêm từ QuantityAvailable
                        // Nếu difference < 0: giảm đặt hàng, cần cộng lại vào QuantityAvailable
                        await UpdateProductQuantityAvailable(
                            new[] { new OrderItemUpdateDto { ProductId = productId, Quantity = absDifference } },
                            difference > 0
                        );
                    }
                }

                // Cập nhật lại tổng tiền đơn hàng
                order.TotalAmount = orderUpdateDto.OrderItems
                    .Sum(i => (i.Quantity ?? 0) * (i.UnitPrice ?? 0) - (i.DiscountAmount ?? 0));

                order.UpdatedAt = now;

                // Cập nhật Order ở repository
                await _unitOfWork.OrderRepository
                    .UpdateAsync(order);

                // Lưu thay đổi vào database
                var result = await _unitOfWork
                    .SaveChangesAsync();

                if (result > 0)
                {
                    // Lấy lại order sau update để trả DTO
                    var updatedOrder = await _unitOfWork.OrderRepository.GetByIdAsync(
                        predicate: o => 
                           o.OrderId == orderUpdateDto.OrderId &&
                           !o.IsDeleted,
                        include: query => query
                           .Include(o => o.DeliveryBatch)
                              .ThenInclude(b => b.Contract)
                           .Include(o => o.OrderItems.Where(i => !i.IsDeleted))
                              .ThenInclude(i => i.Product),
                        asNoTracking: true
                    );

                    if (updatedOrder != null)
                    {
                        // Ánh xạ thực thể đã lưu sang DTO phản hồi
                        var responseDto = updatedOrder.MapToOrderViewDetailsDto();

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

        public async Task<IServiceResult> DeleteOrderById(Guid orderId, Guid userId)
        {
            try
            {
                // Chỉ chấp nhận BusinessManager
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m => 
                       m.UserId == userId && 
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Bạn không phải BusinessManager nên không có quyền xóa đơn hàng."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm Order theo ID kèm danh sách OrderItems
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o =>
                       o.OrderId == orderId &&
                       !o.IsDeleted &&
                       o.DeliveryBatch != null &&
                       o.DeliveryBatch.Contract != null &&
                       o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(o => o.OrderItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                // Xóa từng OrderItem trước (nếu có)
                if (order.OrderItems != null &&
                    order.OrderItems.Any())
                {
                    // Cập nhật QuantityAvailable của Products (cộng lại số lượng đã đặt)
                    var orderItemsForUpdate = order.OrderItems
                        .Where(oi => !oi.IsDeleted)
                        .Select(oi => new OrderItemUpdateDto
                        {
                            ProductId = oi.ProductId,
                            Quantity = oi.Quantity
                        });
                    
                    await UpdateProductQuantityAvailable(orderItemsForUpdate, false);

                    foreach (var item in order.OrderItems)
                    {
                        // Xóa OrderItem khỏi repository
                        await _unitOfWork.OrderItemRepository
                            .RemoveAsync(item);
                    }
                }

                    // Xóa Order khỏi repository
                    await _unitOfWork.OrderRepository
                        .RemoveAsync(order);

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

        public async Task<IServiceResult> SoftDeleteOrderById(Guid orderId, Guid userId)
        {
            try
            {
                // Kiểm tra BusinessManager từ userId
                var manager = await _unitOfWork.BusinessManagerRepository.GetByIdAsync(
                    predicate: m =>
                       m.UserId == userId &&
                       !m.IsDeleted,
                    asNoTracking: true
                );

                if (manager == null)
                {
                    return new ServiceResult(
                        Const.FAIL_DELETE_CODE,
                        "Bạn không phải BusinessManager nên không có quyền xóa mềm đơn hàng."
                    );
                }

                var managerId = manager.ManagerId;

                // Tìm order theo ID
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(
                    predicate: o => 
                       o.OrderId == orderId && 
                       !o.IsDeleted &&
                       o.DeliveryBatch != null &&
                       o.DeliveryBatch.Contract != null &&
                       o.DeliveryBatch.Contract.SellerId == managerId,
                    include: query => query
                       .Include(o => o.OrderItems),
                    asNoTracking: false
                );

                // Kiểm tra nếu không tồn tại
                if (order == null)
                {
                    return new ServiceResult(
                        Const.WARNING_NO_DATA_CODE,
                        Const.WARNING_NO_DATA_MSG
                    );
                }
                else
                {
                    // Cập nhật QuantityAvailable của Products (cộng lại số lượng đã đặt)
                    var orderItemsForUpdate = order.OrderItems
                        .Where(oi => !oi.IsDeleted)
                        .Select(oi => new OrderItemUpdateDto
                        {
                            ProductId = oi.ProductId,
                            Quantity = oi.Quantity
                        });
                    
                    await UpdateProductQuantityAvailable(orderItemsForUpdate, false);

                    // Đánh dấu order là đã xóa
                    order.IsDeleted = true;
                    order.UpdatedAt = DateHelper.NowVietnamTime();

                    // Đánh dấu tất cả OrderItems là đã xóa
                    foreach (var orderItem in order.OrderItems.Where(i => !i.IsDeleted))
                    {
                        orderItem.IsDeleted = true;
                        orderItem.UpdatedAt = DateHelper.NowVietnamTime();

                        // Đảm bảo EF theo dõi thay đổi của orderItem
                        await _unitOfWork.OrderItemRepository
                            .UpdateAsync(orderItem);
                    }

                    // Cập nhật xoá mềm order ở repository
                    await _unitOfWork.OrderRepository
                        .UpdateAsync(order);

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

        /// <summary>
        /// Cập nhật QuantityAvailable của Products dựa trên OrderItems
        /// </summary>
        /// <param name="orderItems">Danh sách OrderItems</param>
        /// <param name="isReserve">True: trừ số lượng (đặt hàng), False: cộng số lượng (hủy đặt hàng)</param>
        private async Task UpdateProductQuantityAvailable(IEnumerable<OrderItemCreateDto> orderItems, bool isReserve)
        {
            try
            {
                // Gom nhóm theo ProductId và tính tổng số lượng
                var productQuantities = orderItems
                    .GroupBy(item => item.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(item => item.Quantity ?? 0)
                    );

                foreach (var kvp in productQuantities)
                {
                    var productId = kvp.Key;
                    var quantity = kvp.Value;

                    // Lấy Product hiện tại
                    var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                        predicate: p => 
                           p.ProductId == productId && 
                           !p.IsDeleted,
                        asNoTracking: false
                    );

                    if (product != null)
                    {
                        // Cập nhật QuantityAvailable
                        if (isReserve)
                        {
                            // Trừ số lượng khi đặt hàng
                            product.QuantityAvailable = Math.Max(0, (product.QuantityAvailable ?? 0) - quantity);
                        }
                        else
                        {
                            // Cộng số lượng khi hủy đặt hàng
                            product.QuantityAvailable = (product.QuantityAvailable ?? 0) + quantity;
                        }

                        product.UpdatedAt = DateHelper.NowVietnamTime();

                        // Cập nhật Product
                        await _unitOfWork.ProductRepository.UpdateAsync(product);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không làm gián đoạn luồng chính
                Console.WriteLine($"Error updating product quantity available: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật QuantityAvailable của Products dựa trên OrderItems (cho Update)
        /// </summary>
        /// <param name="orderItems">Danh sách OrderItems</param>
        /// <param name="isReserve">True: trừ số lượng (đặt hàng), False: cộng số lượng (hủy đặt hàng)</param>
        private async Task UpdateProductQuantityAvailable(IEnumerable<OrderItemUpdateDto> orderItems, bool isReserve)
        {
            try
            {
                // Gom nhóm theo ProductId và tính tổng số lượng
                var productQuantities = orderItems
                    .GroupBy(item => item.ProductId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(item => item.Quantity ?? 0)
                    );

                foreach (var kvp in productQuantities)
                {
                    var productId = kvp.Key;
                    var quantity = kvp.Value;

                    // Lấy Product hiện tại
                    var product = await _unitOfWork.ProductRepository.GetByIdAsync(
                        predicate: p => 
                           p.ProductId == productId && 
                           !p.IsDeleted,
                        asNoTracking: false
                    );

                    if (product != null)
                    {
                        // Cập nhật QuantityAvailable
                        if (isReserve)
                        {
                            // Trừ số lượng khi đặt hàng
                            product.QuantityAvailable = Math.Max(0, (product.QuantityAvailable ?? 0) - quantity);
                        }
                        else
                        {
                            // Cộng số lượng khi hủy đặt hàng
                            product.QuantityAvailable = (product.QuantityAvailable ?? 0) + quantity;
                        }

                        product.UpdatedAt = DateHelper.NowVietnamTime();

                        // Cập nhật Product
                        await _unitOfWork.ProductRepository.UpdateAsync(product);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không làm gián đoạn luồng chính
                Console.WriteLine($"Error updating product quantity available: {ex.Message}");
            }
        }
    }
}
