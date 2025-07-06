using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.OrderEnums;
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
                DeliveryBatchCode = order.DeliveryBatch?.DeliveryBatchCode ?? string.Empty,
                ContractNumber = order.DeliveryBatch?.Contract?.ContractNumber ?? string.Empty,
                OrderItems = order.OrderItems?
                    .Where(item => !item.IsDeleted)
                    .Select(item => new OrderItemViewDto
                    {
                        OrderItemId = item.OrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.ProductName ?? string.Empty,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = item.DiscountAmount,
                        TotalPrice = item.TotalPrice,
                        Note = item.Note ?? string.Empty
                    })
                    .ToList() ?? new List<OrderItemViewDto>()
            };
        }
    }
}
