using Application.DTOs.Subcategory;

namespace Application.Services.Interfaces
{
    public interface ISubcategoryService
    {
        Task<IEnumerable<ReadSubcategoryDto>> GetAllSubcategoriesAsync();
    }
}
