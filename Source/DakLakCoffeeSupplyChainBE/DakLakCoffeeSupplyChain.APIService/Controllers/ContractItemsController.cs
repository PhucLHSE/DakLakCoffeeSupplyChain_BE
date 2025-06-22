using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Repositories.Models;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "BusinessManager")]
    public class ContractItemsController : ControllerBase
    {
        private readonly IContractItemService _contractItemService;

        public ContractItemsController(IContractItemService contractItemService)
            => _contractItemService = contractItemService;

        // PATCH: api/<ContractItemsController>/soft-delete/{contractItemId}
        [HttpPatch("soft-delete/{contractItemId}")]
        public async Task<IActionResult> SoftDeleteContractItemByIdAsync(Guid contractItemId)
        {
            var result = await _contractItemService.SoftDeleteContractItemById(contractItemId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy mục sản phẩm trong hợp đồng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
