using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stem.Business.Business;
using Stem.Common;
using Stem.Data.DTO;
using Stem.Data.Models;

namespace SWP391__StempedeKit_FA24.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductBusiness _productBusiness;

        public ProductsController(IProductBusiness productBusiness)
        {
            _productBusiness = productBusiness;
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var products = await _productBusiness.GetAll();

            // Check result status
            if (products.Status == Const.SUCCESS_READ_CODE)
            {
                // Return only product list without related entities
                return Ok(products.Data);
            }

            return NotFound(new { Status = products.Status, Message = products.Message });
        }
        // GET: api/products/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productBusiness.GetById(id);

            // Check result status
            if (result.Status == Const.SUCCESS_READ_CODE)
            {
                return Ok(result.Data); // Return the product
            }

            return NotFound(new { Status = result.Status, Message = result.Message });
        }


     /*   [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto productDto)

        {

            if (productDto == null)
            {
                return BadRequest(new { Status = Const.FAIL_CREATE_CODE, Message = "Product cannot be null" });
            }
            var result = await _productBusiness.Created(
                productDto.ProductName,
                productDto.Price,
                productDto.StockQuantity,
                productDto.Description,
                productDto.Ages,
                productDto.SupportInstances,
                productDto.LabName,
                productDto.LabSchedule,
                productDto.LabUrl
            );

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                return CreatedAtAction(nameof(GetById), new { id = productDto.ProductId }, result.Data);
            }
            else
            {
                return BadRequest(new { Status = result.Status, Message = result.Message });
            }
        }*/
        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest(new { Status = Const.FAIL_CREATE_CODE, Message = "Product cannot be null" });
            }

            var result = await _productBusiness.Create(product);

            // Check result status
            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, result.Data);
            }

            return BadRequest(new { Status = result.Status, Message = result.Message });
        }

        // PUT: api/products/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            if (product == null || product.ProductId != id)
            {
                return BadRequest(new { Status = Const.FAIL_UPDATE_CODE, Message = "Product ID mismatch" });
            }

            // Ensure all fields of the product are updated
            var existingProduct = await _productBusiness.GetById(id);
            if (existingProduct.Status != Const.SUCCESS_READ_CODE)
            {
                return NotFound(new { Status = existingProduct.Status, Message = "Product not found" });
            }

            var result = await _productBusiness.Update(product);

            // Check result status
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
            {
                return NoContent(); // Successful update, return 204 NoContent
            }

            return BadRequest(new { Status = result.Status, Message = result.Message });
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productBusiness.DeleteById(id);

            // Check result status
            if (result.Status == Const.SUCCESS_DELETE_CODE)
            {
                return NoContent(); // Successfully deleted
            }

            return NotFound(new { Status = result.Status, Message = result.Message });
        }
    }
}
    