using Stemkit.DTOs.Subcategory;

namespace Stemkit.Services.Interfaces
{
    public interface ISubcategoryService
    {
        Task<IEnumerable<ReadSubcategoryDto>> GetAllSubcategoriesAsync();
    }
}
