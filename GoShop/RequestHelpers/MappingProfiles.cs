using AutoMapper;
using GoShop.DTOs;
using GoShop.Entities;

namespace GoShop.RequestHelpers
{
    public class MappingProfiles:Profile
    {
        public MappingProfiles()
        {
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}
