using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NguyenHoangPhuoc_AttendanceAPI.Models;
using NguyenHoangPhuoc_AttendanceAPI.Repositories;

namespace NguyenHoangPhuoc_AttendanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductApiController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductApiController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm" });

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            await _productRepository.AddAsync(product);
            return CreatedAtAction(
                nameof(GetProductById),
                new { id = product.Id },
                product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(
            int id, [FromBody] Product product)
        {
            if (id != product.Id)
                return BadRequest(new {
                    message = "ID trên URL không khớp ID sản phẩm"
                });

            var existing = await _productRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm" });

            await _productRepository.UpdateAsync(product);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var existing = await _productRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm" });

            await _productRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
