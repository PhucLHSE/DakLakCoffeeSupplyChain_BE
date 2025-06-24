using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
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
    [Authorize(Roles = "BusinessManager")]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractsController(IContractService contractService)
            => _contractService = contractService;

        // GET: api/<ContractsController>
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllContractsAsync()
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

            var result = await _contractService.GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET api/<ContractsController>/{contractId}
        [HttpGet("{contractId}")]
        public async Task<IActionResult> GetById(Guid contractId)
        {
            var result = await _contractService.GetById(contractId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<ContractsController>
        [HttpPost]
        public async Task<IActionResult> CreateContractAsync([FromBody] ContractCreateDto contractCreateDto)
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

            var result = await _contractService.Create(contractCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { contractId = ((ContractViewDetailDto)result.Data).ContractId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // DELETE api/<ContractsController>/{contractId}
        [HttpDelete("{contractId}")]
        public async Task<IActionResult> DeleteContractByIdAsync(Guid contractId)
        {
            var result = await _contractService.DeleteContractById(contractId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy hợp đồng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/<ContractsController>/soft-delete/{contractId}
        [HttpPatch("soft-delete/{contractId}")]
        public async Task<IActionResult> SoftDeleteContractByIdAsync(Guid contractId)
        {
            var result = await _contractService.SoftDeleteContractById(contractId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy hợp đồng.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }

        private async Task<bool> ContractExistsAsync(Guid contractId)
        {
            var result = await _contractService.GetById(contractId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
