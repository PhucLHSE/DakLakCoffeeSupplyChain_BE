using DakLakCoffeeSupplyChain.Common.DTOs.OrderDTOs.OrderItemDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class OrderItemMapper
    {
        // View Mapper: OrderItem → OrderItemViewDto
        public static OrderItemViewDto MapToOrderItemViewDto(this OrderItem orderItem)
        {
            return new OrderItemViewDto
            {
                OrderItemId = orderItem.OrderItemId,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.Product?.ProductName ?? string.Empty,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                DiscountAmount = orderItem.DiscountAmount,
                TotalPrice = orderItem.TotalPrice,
                Note = orderItem.Note ?? string.Empty
            };
        }

        // Create Mapper: OrderItemCreateDto → OrderItem
        public static OrderItem MapToNewOrderItem(
            this OrderItemCreateDto dto, 
            double unitPrice, 
            double discountAmount)
        {
            double quantity = dto.Quantity ?? 0;
            double totalPrice = quantity * unitPrice - discountAmount;

            return new OrderItem
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = dto.OrderId,
                ContractDeliveryItemId = dto.ContractDeliveryItemId,
                ProductId = dto.ProductId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                DiscountAmount = discountAmount,
                TotalPrice = totalPrice,
                Note = dto.Note ?? string.Empty,
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                IsDeleted = false
            };
        }

        // Update Mapper: OrderItemUpdateDto → OrderItem (cập nhật thực thể có sẵn)
        public static void MapToUpdateOrderItem(
            this OrderItemUpdateDto dto,
            OrderItem orderItem, 
            double unitPrice, 
            double discountAmount)
        {
            double quantity = dto.Quantity ?? 0;
            double totalPrice = quantity * unitPrice - discountAmount;

            orderItem.OrderId = dto.OrderId;
            orderItem.ContractDeliveryItemId = dto.ContractDeliveryItemId;
            orderItem.ProductId = dto.ProductId;
            orderItem.Quantity = quantity;
            orderItem.UnitPrice = unitPrice;
            orderItem.DiscountAmount = discountAmount;
            orderItem.TotalPrice = totalPrice;
            orderItem.Note = dto.Note ?? string.Empty;
            orderItem.UpdatedAt = DateHelper.NowVietnamTime();
        }
    }
}
