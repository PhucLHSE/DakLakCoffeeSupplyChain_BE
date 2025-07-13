using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs.ContractDeliveryItem;
using DakLakCoffeeSupplyChain.Services.IServices;
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

        // POST api/<ContractItemsController>
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> CreateContractDeliveryItemAsync([FromBody] ContractDeliveryItemCreateDto contractDeliveryItemCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _contractDeliveryItemService.Create(contractDeliveryItemCreateDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(201, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<ContractDeliveryItemsController>/{deliveryItemId}
        [HttpPut("{deliveryItemId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> UpdateContractItemAsync(Guid deliveryItemId, [FromBody] ContractDeliveryItemUpdateDto contractDeliveryItemUpdate)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (deliveryItemId != contractDeliveryItemUpdate.DeliveryItemId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _contractDeliveryItemService.Update(contractDeliveryItemUpdate);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy mục cần cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<ContractDeliveryItemsController>/{deliveryItemId}
        [HttpDelete("{deliveryItemId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> DeleteContractItemByIdAsync(Guid deliveryItemId)
        {
            var result = await _contractDeliveryItemService.DeleteContractDeliveryItemById(deliveryItemId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy lô giao hàng thuộc hợp đồng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

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
