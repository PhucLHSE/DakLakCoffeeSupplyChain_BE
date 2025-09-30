using DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class PaymentMapper
    {
        /// <summary>
        /// Map Payment entity to a PaymentHistoryDto.
        /// </summary>
        public static PaymentHistoryDto ToPaymentHistoryDto(this Payment payment)
        {
            return new PaymentHistoryDto
            {
                PaymentId = payment.PaymentId,
                PaymentPurpose = payment.PaymentPurpose,
                PaymentStatus = payment.PaymentStatus,
                PaymentMethod = payment.PaymentMethod,
                PaymentAmount = payment.PaymentAmount,
                CreatedAt = payment.CreatedAt,
                PaymentTime = payment.PaymentTime,
                RelatedEntityId = payment.RelatedEntityId,
                PaymentCode = payment.PaymentCode
            };
        }
    }
}
