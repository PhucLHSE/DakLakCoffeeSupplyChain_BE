using DakLakCoffeeSupplyChain.Common.DTOs.PaymentDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(PaymentCreateDto dto);
        Task<bool> HandleWebhookAsync(PayOSWebhookDto dto);
    }
}
