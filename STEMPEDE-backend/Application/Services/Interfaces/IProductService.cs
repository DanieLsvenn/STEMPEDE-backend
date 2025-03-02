using Application.DTOs;
using Application.DTOs.Product;
using Application.Utils.Implementation;

namespace Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ReadProductDto?> GetProductByIdAsync(int productId);
        Task<PaginatedList<ReadProductDto>> GetAllProductsAsync(QueryParameters queryParameters);
        Task<ReadProductDto> CreateProductAsync(CreateProductDto createDto);
        Task<bool> UpdateProductAsync(int productId, UpdateProductDto updateDto);
        Task<bool> DeleteProductAsync(int productId);
    }
}
