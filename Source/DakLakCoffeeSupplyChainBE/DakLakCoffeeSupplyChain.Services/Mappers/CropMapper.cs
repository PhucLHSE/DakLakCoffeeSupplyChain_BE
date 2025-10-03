using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.ContractEnums;
using DakLakCoffeeSupplyChain.Common.Enum.CropEnums;
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
    public static class CropMapper
    {
        // Mapper Crop -> CropViewAllDto
        public static CropViewAllDto MapToCropViewAllDto(this Crop crop)
        {
            // Parse Status string to enum
            CropStatus status = Enum.TryParse<CropStatus>
                (crop.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : CropStatus.Inactive;

            return new CropViewAllDto
            {
                CropId = crop.CropId,
                CropCode = crop.CropCode ?? string.Empty,
                Address = crop.Address ?? string.Empty,
                FarmName = crop.FarmName ?? string.Empty,
                CropArea = crop.CropArea,
                Status = status,
                Note = crop.Note?? string.Empty,
                IsApproved = crop.IsApproved,
            };
        }

        // Mapper Crop -> CropViewDetailsDto
        public static CropViewDetailsDto MapToCropViewDetailsDto(this Crop crop)
        {
            // Parse Status string to enum
            CropStatus status = Enum.TryParse<CropStatus>
                (crop.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : CropStatus.Inactive;

            return new CropViewDetailsDto
            {
                CropId = crop.CropId,
                CropCode = crop.CropCode ?? string.Empty,
                Address = crop.Address ?? string.Empty,
                FarmName = crop.FarmName ?? string.Empty,
                CropArea = crop.CropArea,
                Status = status,
                CreatedAt = crop.CreatedAt,
                UpdatedAt = crop.UpdatedAt,
                CreatedBy = crop.CreatedBy,
                UpdatedBy = crop.UpdatedBy,
                IsDeleted = crop.IsDeleted,
                Note = crop.Note ?? string.Empty,
                IsApproved = crop.IsApproved,
                ApprovedAt = crop.ApprovedAt,
                ApprovedBy = crop.ApprovedBy,
                RejectReason = crop.RejectReason ?? string.Empty,
                ApprovedByName = crop.ApprovedByNavigation?.Name,
                CreatedByName = crop.CreatedByNavigation?.User?.Name,
                UpdatedByName = crop.UpdatedByNavigation?.User?.Name
            };
        }

        // Mapper CropCreateDto -> Crop
        public static Crop MapToCreateCrop(this CropCreateDto dto, Guid createdBy, string cropCode)
        {
            return new Crop
            {
                CropId = Guid.NewGuid(),
                CropCode = cropCode, // From service
                Address = dto.Address,
                FarmName = dto.FarmName,
                CropArea = dto.CropArea,
                Status = dto.Status.ToString(), // Auto set Active khi tạo mới
                CreatedAt = DateHelper.NowVietnamTime(),
                UpdatedAt = DateHelper.NowVietnamTime(),
                CreatedBy = createdBy,
                UpdatedBy = createdBy,
                IsDeleted = false,
                Note = dto.Note ?? string.Empty,
                IsApproved = false,  
            };
        }

        // Mapper CropUpdateDto -> Crop (for update)
        public static void MapToUpdateCrop(this CropUpdateDto dto, Crop crop, Guid updatedBy)
        {
            crop.CropCode = dto.CropCode;
            crop.Address = dto.Address;
            crop.FarmName = dto.FarmName;
            crop.CropArea = dto.CropArea;
            // Status không update thủ công, auto transition theo workflow
            // crop.Status = dto.Status;
            crop.UpdatedAt = DateHelper.NowVietnamTime();
            crop.UpdatedBy = updatedBy;
            crop.Note = dto.Note ?? string.Empty;
        }
    }
}
