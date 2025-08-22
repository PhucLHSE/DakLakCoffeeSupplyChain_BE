using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs;
using DakLakCoffeeSupplyChain.Common.DTOs.ContractDTOs.ContractItemDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.Http;

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

            var result = await _contractService
                .GetAll(userId);

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

            var result = await _contractService
                .GetById(contractId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        // POST api/<ContractsController>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateContractAsync(
            [FromForm] ContractCreateDto contractCreateDto)
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

            var result = await _contractService
                .Create(contractCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { contractId = ((ContractViewDetailsDto)result.Data).ContractId },
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT api/<ContractsController>/{contractId}
        [HttpPut("{contractId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateContractAsync(
            Guid contractId, 
            [FromForm] ContractUpdateDto contractUpdateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (contractId != contractUpdateDto.ContractId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            // Kiểm tra quyền sở hữu hợp đồng
            var ownershipCheck = await _contractService
                .GetById(contractId, userId);

            if (ownershipCheck.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy hợp đồng hoặc bạn không có quyền truy cập.");

            if (ownershipCheck.Status != Const.SUCCESS_READ_CODE)
                return StatusCode(500, ownershipCheck.Message);

            // Nếu user sở hữu hợp đồng, tiến hành update
            var result = await _contractService
                .Update(contractUpdateDto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy hợp đồng.");

            return StatusCode(500, result.Message);
        }

        // DELETE api/<ContractsController>/{contractId}
        [HttpDelete("{contractId}")]
        public async Task<IActionResult> DeleteContractByIdAsync(Guid contractId)
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

            // Kiểm tra quyền sở hữu hợp đồng
            var ownershipCheck = await _contractService
                .GetById(contractId, userId);

            if (ownershipCheck.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy hợp đồng hoặc bạn không có quyền truy cập.");

            if (ownershipCheck.Status != Const.SUCCESS_READ_CODE)
                return StatusCode(500, ownershipCheck.Message);

            // Nếu vượt qua kiểm tra quyền, tiến hành xóa
            var result = await _contractService
                .DeleteContractById(contractId);

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

            // Kiểm tra quyền sở hữu hợp đồng
            var ownershipCheck = await _contractService
                .GetById(contractId, userId);

            if (ownershipCheck.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy hợp đồng hoặc bạn không có quyền truy cập.");

            if (ownershipCheck.Status != Const.SUCCESS_READ_CODE)
                return StatusCode(500, ownershipCheck.Message);

            // Nếu vượt qua kiểm tra quyền, tiến hành xóa mềm
            var result = await _contractService
                .SoftDeleteContractById(contractId);

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
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return false; // Không xác định được user → không kiểm tra được
            }

            var result = await _contractService
                .GetById(contractId, userId);

            return result.Status == Const.SUCCESS_READ_CODE;
        }
    }
}
