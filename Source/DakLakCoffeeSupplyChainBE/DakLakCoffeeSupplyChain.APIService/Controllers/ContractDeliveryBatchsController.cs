using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Authorization;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDeliveryBatchDTOs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractDeliveryBatchsController : ControllerBase
    {
        private readonly IContractDeliveryBatchService _contractDeliveryBatchService;

        public ContractDeliveryBatchsController(IContractDeliveryBatchService contractDeliveryBatchService)
            => _contractDeliveryBatchService = contractDeliveryBatchService;

        // GET: api/<ContractDeliveryBatchsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetAllContractDeliveryBatchsAsync()
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

            var result = await _contractDeliveryBatchService.GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/<ContractDeliveryBatchsController>/{deliveryBatchId}
        [HttpGet("{deliveryBatchId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetById(Guid deliveryBatchId)
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

            var result = await _contractDeliveryBatchService.GetById(deliveryBatchId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<ContractDeliveryBatchsController>
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> CreateContractDeliveryBatchAsync([FromBody] ContractDeliveryBatchCreateDto contractDeliveryBatchDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            var result = await _contractDeliveryBatchService.Create(contractDeliveryBatchDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { deliveryBatchId = ((ContractDeliveryBatchViewDetailsDto)result.Data).DeliveryBatchId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<ContractDeliveryBatchsController>/{deliveryBatchId}
        [HttpPut("{deliveryBatchId}")]
        public async Task<IActionResult> UpdateContractDeliveryBatchAsync(Guid deliveryBatchId, [FromBody] ContractDeliveryBatchUpdateDto contractDeliveryBatchDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (deliveryBatchId != contractDeliveryBatchDto.DeliveryBatchId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            var result = await _contractDeliveryBatchService.Update(contractDeliveryBatchDto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đợt giao hàng.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<ContractDeliveryBatchsController>/{deliveryBatchId}
        [HttpDelete("{deliveryBatchId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> DeleteContractByIdAsync(Guid deliveryBatchId)
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

            var result = await _contractDeliveryBatchService.DeleteContractDeliveryBatchById(deliveryBatchId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy đợt giao hàng cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<ContractDeliveryBatchsController>/soft-delete/{deliveryBatchId}
        [HttpPatch("soft-delete/{deliveryBatchId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteContractDeliveryBatchByIdAsync(Guid deliveryBatchId)
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

            var result = await _contractDeliveryBatchService.SoftDeleteContractDeliveryBatchById(deliveryBatchId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy lô giao hàng hợp đồng cần xóa.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> ContractDeliveryBatchExistsAsync(Guid deliveryBatchId)
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return false;
            }

            var result = await _contractDeliveryBatchService.GetById(deliveryBatchId, userId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
