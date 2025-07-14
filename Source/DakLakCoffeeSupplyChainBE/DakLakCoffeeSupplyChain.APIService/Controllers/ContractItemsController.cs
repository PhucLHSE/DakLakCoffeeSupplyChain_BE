using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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


        // POST api/<ContractItemsController>
        [HttpPost]
        public async Task<IActionResult> CreateContractItemAsync([FromBody] ContractItemCreateDto contractItemCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _contractItemService.Create(contractItemCreateDto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(201, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<ContractItemsController>/{contractItemId}
        [HttpPut("{contractItemId}")]
        public async Task<IActionResult> UpdateContractItemAsync(Guid contractItemId, [FromBody] ContractItemUpdateDto contractItemUpdateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (contractItemId != contractItemUpdateDto.ContractItemId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _contractItemService.Update(contractItemUpdateDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy sản phẩm hợp đồng.");

            return StatusCode(500, result.Message);
        }


        // DELETE api/<ContractItemsController>/{contractItemId}
        [HttpDelete("{contractItemId}")]
        public async Task<IActionResult> DeleteContractItemByIdAsync(Guid contractItemId)
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _contractItemService.DeleteContractItemById(contractItemId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy sản phẩm hợp đồng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<ContractItemsController>/soft-delete/{contractItemId}
        [HttpPatch("soft-delete/{contractItemId}")]
        public async Task<IActionResult> SoftDeleteContractItemByIdAsync(Guid contractItemId)
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _contractItemService.SoftDeleteContractItemById(contractItemId, userId);

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
