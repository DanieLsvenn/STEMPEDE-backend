using static System.Runtime.InteropServices.JavaScript.JSType;
using Stemkit.DTOs.Product;
using Stemkit.Models;
using AutoMapper;

namespace Stemkit.Configurations
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Product, ReadProductDto>()
                .ForMember(dest => dest.LabName, opt => opt.MapFrom(src => src.Lab.LabName))
                .ForMember(dest => dest.SubcategoryName, opt => opt.MapFrom(src => src.Subcategory.SubcategoryName));

            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}
