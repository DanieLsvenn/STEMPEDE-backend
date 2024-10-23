using Stemkit.DTOs;
using Stemkit.DTOs.Product;

namespace Stemkit.Services.Interfaces
{
    public interface IProductService
    {
        Task<ReadProductDto?> GetProductByIdAsync(int productId);
        Task<PagedResult<ReadProductDto>> GetAllProductsAsync(ProductQueryParameters queryParameters);
        Task<ReadProductDto> CreateProductAsync(CreateProductDto createDto);
        Task<bool> UpdateProductAsync(int productId, UpdateProductDto updateDto);
        Task<bool> DeleteProductAsync(int productId);
    }
}
