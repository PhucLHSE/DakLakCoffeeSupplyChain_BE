using DakLakCoffeeSupplyChain.Common.DTOs.CoffeeTypeDTOs;
using DakLakCoffeeSupplyChain.Repositories.Models;

namespace DakLakCoffeeSupplyChain.Services.Mappers
{
    public static class CoffeeTypeMapper
    {
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
    }
}
