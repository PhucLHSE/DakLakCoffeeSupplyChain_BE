using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractDeliveryItemsController : ControllerBase
    {
        private readonly IContractDeliveryItemService _contractDeliveryItemService;

        public ContractDeliveryItemsController(IContractDeliveryItemService contractDeliveryItemService)
            => _contractDeliveryItemService = contractDeliveryItemService;

        // PATCH: api/<ContractDeliveryItemsController>/soft-delete/{deliveryItemId}
        [HttpPatch("soft-delete/{deliveryItemId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteContractDeliveryItemByIdAsync(Guid deliveryItemId)
        {
            var result = await _contractDeliveryItemService.SoftDeleteContractDeliveryItemById(deliveryItemId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy lô giao hàng thuộc hợp đồng với mã được cung cấp.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
