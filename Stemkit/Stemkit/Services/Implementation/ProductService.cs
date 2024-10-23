using Stemkit.Data;
using Stemkit.DTOs.Product;
using Stemkit.Models;
using AutoMapper;
using Stemkit.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stemkit.DTOs;

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

        public async Task<PagedResult<ReadProductDto>> GetAllProductsAsync(ProductQueryParameters queryParameters)
        {
            IQueryable<Product> query = _unitOfWork.GetRepository<Product>().Query(includeProperties: "Lab,Subcategory");

            // Filtering
            if (queryParameters.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= queryParameters.MinPrice.Value);
            }

            if (queryParameters.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= queryParameters.MaxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.ProductName))
            {
                var productName = queryParameters.ProductName.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(productName));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.SubcategoryName))
            {
                var subcategoryName = queryParameters.SubcategoryName.Trim().ToLower();
                query = query.Where(p => p.Subcategory.SubcategoryName.ToLower().Contains(subcategoryName));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.LabName))
            {
                var labName = queryParameters.LabName.Trim().ToLower();
                query = query.Where(p => p.Lab.LabName.ToLower().Contains(labName));
            }

            if (!string.IsNullOrWhiteSpace(queryParameters.Ages))
            {
                var ages = queryParameters.Ages.Trim().ToLower();
                query = query.Where(p => p.Ages.ToLower().Contains(ages));
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(queryParameters.SortBy))
            {
                bool descending = string.Equals(queryParameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

                switch (queryParameters.SortBy.ToLower())
                {
                    case "price":
                        query = descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price);
                        break;
                    case "name":
                        query = descending ? query.OrderByDescending(p => p.ProductName) : query.OrderBy(p => p.ProductName);
                        break;
                    case "stockquantity":
                        query = descending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity);
                        break;
                    case "supportinstances":
                        query = descending ? query.OrderByDescending(p => p.SupportInstances) : query.OrderBy(p => p.SupportInstances);
                        break;
                    default:
                        query = query.OrderBy(p => p.ProductId); // Default sorting
                        break;
                }
            }
            else
            {
                query = query.OrderBy(p => p.ProductId); // Default sorting
            }

            // Pagination
            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            var productDtos = _mapper.Map<IEnumerable<ReadProductDto>>(products);

            return new PagedResult<ReadProductDto>
            {
                Items = productDtos,
                TotalCount = totalCount,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
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
