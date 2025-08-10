using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.OrderEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class OrderMapper
    {
        // Mapper OrderViewAllDto
        public static OrderViewAllDto MapToOrderViewAllDto(this Order order)
        {
            // Parse Status string to enum
            OrderStatus status= Enum.TryParse<OrderStatus>(order.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : OrderStatus.Pending;

            return new OrderViewAllDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode ?? string.Empty,
                DeliveryRound = order.DeliveryRound,
                OrderDate = order.OrderDate,
                ActualDeliveryDate = order.ActualDeliveryDate,
                TotalAmount = order.TotalAmount,
                Status = status,
                DeliveryBatchCode = order.DeliveryBatch?.DeliveryBatchCode ?? string.Empty,
                ContractNumber = order.DeliveryBatch?.Contract?.ContractNumber ?? string.Empty
            };
        }

        // Mapper OrderViewDetailsDto
        public static OrderViewDetailsDto MapToOrderViewDetailsDto(this Order order)
        {
            // Parse Status string to enum
            OrderStatus status = Enum.TryParse<OrderStatus>(order.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : OrderStatus.Pending;

            return new OrderViewDetailsDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode ?? string.Empty,
                DeliveryRound = order.DeliveryRound,
                OrderDate = order.OrderDate,
                ActualDeliveryDate = order.ActualDeliveryDate,
                TotalAmount = order.TotalAmount,
                Note = order.Note ?? string.Empty,
                CancelReason = order.CancelReason ?? string.Empty,
                Status = status,
                DeliveryBatchId = order.DeliveryBatchId,
                DeliveryBatchCode = order.DeliveryBatch?.DeliveryBatchCode ?? string.Empty,
                ContractNumber = order.DeliveryBatch?.Contract?.ContractNumber ?? string.Empty,
                OrderItems = order.OrderItems?
                    .Where(item => !item.IsDeleted)
                    .Select(item => new OrderItemViewDto
                    {
                        OrderItemId = item.OrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.ProductName ?? string.Empty,
                        ContractDeliveryItemId = item.ContractDeliveryItemId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        TotalPrice = item.TotalPrice,
                        Note = item.Note ?? string.Empty
                    })
                    .ToList() ?? new List<OrderItemViewDto>()
            };
        }

        // Mapper OrderCreateDto -> Order
        public static Order MapToNewOrder(this OrderCreateDto dto, string orderCode, Guid userId)
        {
            var orderId = Guid.NewGuid();

            var order = new Order
            {
                OrderId = orderId,
                OrderCode = orderCode,
                DeliveryBatchId = dto.DeliveryBatchId,
                DeliveryRound = dto.DeliveryRound,
                OrderDate = dto.OrderDate ?? DateHelper.NowVietnamTime(),
                ActualDeliveryDate = dto.ActualDeliveryDate,
                Note = dto.Note,
                Status = dto.Status.ToString(), // enum to string
                CancelReason = dto.CancelReason,
                CreatedBy = userId,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false,

                OrderItems = dto.OrderItems.Select((item, index) => new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    ContractDeliveryItemId = item.ContractDeliveryItemId,
                    Quantity = item.Quantity ?? 0,
                    UnitPrice = item.UnitPrice ?? 0,
                    DiscountAmount = item.DiscountAmount ?? 0,
                    TotalPrice = (item.Quantity ?? 0) * (item.UnitPrice ?? 0) - (item.DiscountAmount ?? 0),
                    Note = item.Note,
                    CreatedAt = DateHelper.NowVietnamTime(),
                    UpdatedAt = DateHelper.NowVietnamTime(),
                    IsDeleted = false
                }).ToList()
            };

            return order;
        }

        // Mapper OrderUpdateDto -> Order
        public static void MapToUpdatedOrder(this OrderUpdateDto dto, Order order)
        {
            order.DeliveryBatchId = dto.DeliveryBatchId;
            order.DeliveryRound = dto.DeliveryRound;
            order.OrderDate = dto.OrderDate ?? order.OrderDate; // giữ nguyên nếu null
            order.ActualDeliveryDate = dto.ActualDeliveryDate;
            order.Note = dto.Note;
            order.Status = dto.Status.ToString(); // enum -> string
            order.CancelReason = dto.CancelReason;
            order.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
