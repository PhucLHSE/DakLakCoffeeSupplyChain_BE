using AutoMapper;
using CoffeeManagement.Flow4.DTOs;
using CoffeeManagement.Flow4.Repositories.Models;

namespace CoffeeManagement.Flow4.APIService.MappingProfiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<WarehouseInboundRequestCreateDto, WarehouseInboundRequest>()
                 .ForMember(dest => dest.PreferredDeliveryDate, opt => opt.MapFrom(src =>
                     src.PreferredDeliveryDate.HasValue ? DateOnly.FromDateTime(src.PreferredDeliveryDate.Value) : (DateOnly?)null));

            // Entity -> DTO
            CreateMap<WarehouseInboundRequest, WarehouseInboundRequestDto>()
                .ForMember(dest => dest.PreferredDeliveryDate, opt => opt.MapFrom(src =>
                    src.PreferredDeliveryDate.HasValue ? src.PreferredDeliveryDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
                .ForMember(dest => dest.ActualDeliveryDate, opt => opt.MapFrom(src =>
                    src.ActualDeliveryDate.HasValue ? src.ActualDeliveryDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null));
        }
    }
    }
