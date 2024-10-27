using Stemkit.Data;
using Stemkit.DTOs.Product;
using Stemkit.Models;
using AutoMapper;
using Stemkit.Services.Interfaces;
using AutoMapper.QueryableExtensions;
using Stemkit.Utils.Implementation;

namespace Stemkit.Services.Implementation
{
    public class ProductService : IProductService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<ProductService> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ReadProductDto?> GetProductByIdAsync(int productId)
        {
            var product = await _unitOfWork.GetRepository<Product>().GetAsync(
                p => p.ProductId == productId,
                includeProperties: "Lab,Subcategory");

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", productId);
                return null;
            }

            return _mapper.Map<ReadProductDto>(product);
        }

        public async Task<PaginatedList<ReadProductDto>> GetAllProductsAsync(ProductQueryParameters queryParameters)
        {
            var productsQuery = _unitOfWork.GetRepository<Product>().GetAllQueryable(includeProperties: "Lab,Subcategory");

            var mappedQuery = productsQuery.ProjectTo<ReadProductDto>(_mapper.ConfigurationProvider);

            // Create paginated list
            var paginatedList = await PaginatedList<ReadProductDto>.CreateAsync(
                mappedQuery,
                queryParameters.PageNumber,
                queryParameters.PageSize
            );

            return paginatedList;
        }

        public async Task<ReadProductDto> CreateProductAsync(CreateProductDto createDto)
        {
            // Validate LabID
            var lab = await _unitOfWork.GetRepository<Lab>().GetAsync(l => l.LabId == createDto.LabID);
            if (lab == null)
            {
                _logger.LogWarning("Lab with ID {LabID} not found.", createDto.LabID);
                throw new ArgumentException("Invalid LabID.");
            }

            // Validate SubcategoryID
            var subcategory = await _unitOfWork.GetRepository<Subcategory>().GetAsync(s => s.SubcategoryId == createDto.SubcategoryID);
            if (subcategory == null)
            {
                _logger.LogWarning("Subcategory with ID {SubcategoryID} not found.", createDto.SubcategoryID);
                throw new ArgumentException("Invalid SubcategoryID.");
            }

            var product = _mapper.Map<Product>(createDto);
            await _unitOfWork.GetRepository<Product>().AddAsync(product);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Product created with ID {ProductId}.", product.ProductId);

            // Fetch the product with related entities to include in the response
            var createdProduct = await _unitOfWork.GetRepository<Product>().GetAsync(
                p => p.ProductId == product.ProductId,
                includeProperties: "Lab,Subcategory");

            return _mapper.Map<ReadProductDto>(createdProduct);
        }

        public async Task<bool> UpdateProductAsync(int productId, UpdateProductDto updateDto)
        {
            var product = await _unitOfWork.GetRepository<Product>().GetAsync(
                p => p.ProductId == productId,
                includeProperties: "");

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for update.", productId);
                return false;
            }

            // Validate LabID
            var lab = await _unitOfWork.GetRepository<Lab>().GetAsync(l => l.LabId == updateDto.LabID);
            if (lab == null)
            {
                _logger.LogWarning("Lab with ID {LabID} not found.", updateDto.LabID);
                throw new ArgumentException("Invalid LabID.");
            }

            // Validate SubcategoryID
            var subcategory = await _unitOfWork.GetRepository<Subcategory>().GetAsync(s => s.SubcategoryId == updateDto.SubcategoryID);
            if (subcategory == null)
            {
                _logger.LogWarning("Subcategory with ID {SubcategoryID} not found.", updateDto.SubcategoryID);
                throw new ArgumentException("Invalid SubcategoryID.");
            }

            // Map the updated fields
            _mapper.Map(updateDto, product);
            _unitOfWork.GetRepository<Product>().Update(product);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Product with ID {ProductId} updated successfully.", productId);

            return true;
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _unitOfWork.GetRepository<Product>().GetAsync(p => p.ProductId == productId);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for deletion.", productId);
                return false;
            }

            _unitOfWork.GetRepository<Product>().Delete(product);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Product with ID {ProductId} deleted successfully.", productId);

            return true;
        }
    }
}
