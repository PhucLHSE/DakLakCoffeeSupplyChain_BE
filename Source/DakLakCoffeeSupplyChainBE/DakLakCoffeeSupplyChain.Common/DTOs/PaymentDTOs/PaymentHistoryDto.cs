using System;

namespace DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs
{
    /// <summary>
    /// Simplified view of a payment entry for history listings.
    /// </summary>
    public class PaymentHistoryDto
    {
        public Guid PaymentId { get; set; }
        public string PaymentPurpose { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public double PaymentAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaymentTime { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
    }
}
