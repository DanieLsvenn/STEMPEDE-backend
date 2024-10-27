using Stemkit.DTOs.Product;
using Stemkit.Models;
using AutoMapper;
using Stemkit.DTOs.Auth;
using Stemkit.DTOs.Lab;
using Stemkit.DTOs.User;
using Stemkit.DTOs.Subcategory;

namespace Stemkit.Configurations
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<User, ReadUserDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status ? "Active" : "Banned"))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.RoleName).ToList()));

            //Product mappings
            CreateMap<Product, ReadProductDto>()
                .ForMember(dest => dest.LabName, opt => opt.MapFrom(src => src.Lab.LabName))
                .ForMember(dest => dest.SubcategoryName, opt => opt.MapFrom(src => src.Subcategory.SubcategoryName));

            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();

            // MappingsAuthentication
            CreateMap<UserRegistrationDto, User>()
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => true)) // Default status
                .ForMember(dest => dest.IsExternal, opt => opt.MapFrom(src => src.IsExternal))
                .ForMember(dest => dest.ExternalProvider, opt => opt.MapFrom(src => src.ExternalProvider ?? null));

            // Lab Mappings
            CreateMap<Lab, ReadLabDto>()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products));

            CreateMap<CreateLabDto, Lab>();
            CreateMap<UpdateLabDto, Lab>();
            CreateMap<Lab, ReadLabSimpleDto>();

            // Subcategory mappings
            CreateMap<Subcategory, ReadSubcategoryDto>();
        }
    }
}
