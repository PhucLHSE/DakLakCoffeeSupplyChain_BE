using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.UserAccountDTOs;
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
                SpecialtyLevel = entity.SpecialtyLevel
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
                SpecialtyLevel = dto.SpecialtyLevel
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
        }
    }
}
