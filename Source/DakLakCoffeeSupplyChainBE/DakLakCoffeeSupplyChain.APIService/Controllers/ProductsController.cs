using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
            => _productService = productService;

        // GET: api/<ProductsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetAllProductsAsync()
        {
            var result = await _productService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<ProductsController>/{productId}
        [HttpGet("{productId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetById(Guid productId)
        {
            var result = await _productService.GetById(productId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // DELETE api/<ProductsController>/{productId}
        [HttpDelete("{productId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> DeleteProductByIdAsync(Guid productId)
        {
            var result = await _productService.DeleteById(productId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy sản phẩm.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> ProductExistsAsync(Guid productId)
        {
            var result = await _productService.GetById(productId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
