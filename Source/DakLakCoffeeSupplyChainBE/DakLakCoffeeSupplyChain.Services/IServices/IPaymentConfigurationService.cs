using DakLakCoffeeSupplyChain.Common.DTOs.PaymentConfigurationDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IPaymentConfigurationService
    {
        Task<IServiceResult> GetAll();

        Task<IServiceResult> GetById(Guid configId);

        Task<IServiceResult> Create(PaymentConfigurationCreateDto paymentConfigurationCreateDto);

        Task<IServiceResult> Update(PaymentConfigurationUpdateDto paymentConfigurationUpdateDto);

        Task<IServiceResult> DeletePaymentConfigurationById(Guid configId);

        Task<IServiceResult> SoftDeletePaymentConfigurationById(Guid configId);
    }
}
