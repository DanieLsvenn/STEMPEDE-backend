﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stemkit.DTOs;
using Stemkit.DTOs.Product;
using Stemkit.Services.Interfaces;

namespace Stemkit.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with pagination, sorting, and filtering.
        /// </summary>
        [HttpGet("get-all")]
        [AllowAnonymous] // Allows unauthenticated access
        public async Task<ActionResult<PagedResult<ReadProductDto>>> GetAllProducts([FromQuery] ProductQueryParameters queryParameters)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid query parameters for GetAllProducts.");
                return BadRequest(ModelState);
            }

            var pagedProducts = await _productService.GetAllProductsAsync(queryParameters);
            return Ok(pagedProducts);
        }

        /// <summary>
        /// Get a product by ID.
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous] // Allows unauthenticated access

        public async Task<ActionResult<ReadProductDto>> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", id);
                return NotFound(new { Message = $"Product with ID {id} not found." });
            }

            return Ok(product);
        }

        /// <summary>
        /// Create a new product.
        /// </summary>
        [HttpPost("create")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<ActionResult<ReadProductDto>> CreateProduct([FromBody] CreateProductDto createDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product creation attempt.");
                return BadRequest(ModelState);
            }

            try
            {
                var createdProduct = await _productService.CreateProductAsync(createDto);
                return CreatedAtAction(nameof(GetProductById), new { id = createdProduct.ProductID }, createdProduct);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Product creation failed due to invalid data.");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a product.");
                return StatusCode(500, new { Message = "An error occurred while creating the product." });
            }
        }

        /// <summary>
        /// Update an existing product.
        /// </summary>
        [HttpPut("update/{id}")]
        [Authorize(Roles = "Manager,Staff")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid product update attempt for ID {ProductId}.", id);
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _productService.UpdateProductAsync(id, updateDto);
                if (!result)
                {
                    return NotFound(new { Message = $"Product with ID {id} not found." });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Product update failed due to invalid data.");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the product with ID {ProductId}.", id);
                return StatusCode(500, new { Message = "An error occurred while updating the product." });
            }
        }

        /// <summary>
        /// Delete a product by ID.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (!result)
                {
                    return NotFound(new { Message = $"Product with ID {id} not found." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the product with ID {ProductId}.", id);
                return StatusCode(500, new { Message = "An error occurred while deleting the product." });
            }
        }
    }
}
