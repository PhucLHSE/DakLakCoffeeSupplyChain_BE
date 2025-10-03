using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Services.IServices
{
    public interface ICropService
    {
        Task<IServiceResult> GetAllCrops(Guid userId, string userRole);
        Task<IServiceResult> GetCropById(Guid cropId, Guid userId, string userRole);
        Task<IServiceResult> CreateCrop(CropCreateDto cropCreateDto, Guid farmerUserId);
        Task<IServiceResult> UpdateCrop(CropUpdateDto cropUpdateDto, Guid farmerUserId);
        Task<IServiceResult> SoftDeleteCrop(Guid cropId, Guid farmerUserId);
        Task<IServiceResult> HardDeleteCrop(Guid cropId, Guid farmerUserId);
        Task AutoUpdateCropStatusAsync(Guid cropId);
        Task<IServiceResult> ApproveCropAsync(Guid cropId, CropApproveDto dto, Guid adminUserId);
        Task<IServiceResult> RejectCropAsync(Guid cropId, CropRejectDto dto, Guid adminUserId);
    }
}
