using DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface IBusinessStaffService
    {
        Task<IServiceResult> Create(BusinessStaffCreateDto dto, Guid supervisorId);
        Task<IServiceResult> GetByIdAsync(Guid staffId);
    }
}
