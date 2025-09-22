using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.Enum.CoffeeTypeEnums;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CoffeeTypeMapper
    {
        // Mapper CoffeeTypeViewAllDto
        public static CoffeeTypeViewAllDto MapToCoffeeTypeViewAllDto(this CoffeeType entity)
        {
            return new CoffeeTypeViewAllDto
            {
                CoffeeTypeId = entity.CoffeeTypeId,
                TypeCode = entity.TypeCode,
                TypeName = entity.TypeName,
                BotanicalName = entity.BotanicalName,
                Description = entity.Description,
                TypicalRegion = entity.TypicalRegion,
                SpecialtyLevel = entity.SpecialtyLevel,
                Status = EnumHelper.ParseEnumFromString(entity.Status,CoffeeTypeStatus.Unknown),
                CoffeeTypeCategory = entity.CoffeeTypeCategory,
                CoffeeTypeParentId = entity.CoffeeTypeParentId
            };
        }
        // Mapper CoffeeTypeCreateDto
        public static CoffeeType MapToCofeeTypeCreateDto(this CoffeeTypeCreateDto dto, string typeCode)
        {
            return new CoffeeType
            {
                CoffeeTypeId = Guid.NewGuid(),
                TypeCode = typeCode,
                TypeName = dto.TypeName,
                BotanicalName = dto.BotanicalName,
                Description = dto.Description,
                TypicalRegion = dto.TypicalRegion,
                SpecialtyLevel = dto.SpecialtyLevel,
                Status = CoffeeTypeStatus.InActive.ToString(),
                CoffeeTypeCategory = dto.CoffeeTypeCategory,
                CoffeeTypeParentId = dto.CoffeeTypeParentId,
            };
        }
        // Mapper CoffeeTypeUpdateDto
        public static void MapToUpdateCoffeeType(this CoffeeTypeUpdateDto dto, CoffeeType ct)
        {
            ct.TypeName = dto.TypeName;
            ct.BotanicalName = dto.BotanicalName;
            ct.Description = dto.Description;
            ct.TypicalRegion = dto.TypicalRegion;
            ct.SpecialtyLevel = dto.SpecialtyLevel;
            ct.Status = CoffeeTypeStatus.InActive.ToString();
            ct.CoffeeTypeCategory = dto.CoffeeTypeCategory;
            ct.CoffeeTypeParentId = dto.CoffeeTypeParentId;
        }
    }
}
