using DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessStaffsController : ControllerBase
    {
        private readonly IBusinessStaffService _businessStaffService;

        public BusinessStaffsController(IBusinessStaffService businessStaffService)
        {
            _businessStaffService = businessStaffService;
        }

        // POST: api/BusinessStaffs
        [HttpPost]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CreateBusinessStaffAsync([FromBody] BusinessStaffCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid supervisorId))
                return Unauthorized("Không xác định được người dùng từ token.");

            var result = await _businessStaffService.Create(dto, supervisorId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { staffId = result.Data }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message); // 409

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result.Message); // 500

            return StatusCode(500, result.Message); // fallback
        }

        // GET: api/BusinessStaffs
        [HttpGet]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetAllBySupervisor()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized("Không xác định được người dùng từ token.");

            var result = await _businessStaffService.GetAllBySupervisorAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data); // 200

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); // 404

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result.Message); // 500

            return StatusCode(500, result.Message); // fallback
        }

        // GET: api/BusinessStaffs/{staffId}
        [HttpGet("{staffId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> GetById(Guid staffId)
        {
            var result = await _businessStaffService.GetByIdAsync(staffId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data); // 200

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); // 404

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result.Message); // 500

            return StatusCode(500, result.Message); // fallback
        }
        [HttpPut("{staffId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> UpdateBusinessStaff(Guid staffId, [FromBody] BusinessStaffUpdateDto dto)
        {
            if (staffId != dto.StaffId)
                return BadRequest("StaffId trong route không khớp với DTO.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _businessStaffService.Update(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = result.Message, staffId = result.Data });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
        // PATCH api/BusinessStaffs/soft-delete/{staffId}
        [HttpPatch("soft-delete/{staffId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> SoftDeleteStaff(Guid staffId)
        {
            var result = await _businessStaffService.SoftDeleteAsync(staffId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict(new { message = result.Message });

            return StatusCode(500, result.Message);
        }


        // DELETE api/BusinessStaffs/{staffId}
        [HttpDelete("{staffId}")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> HardDeleteStaff(Guid staffId)
        {
            var result = await _businessStaffService.HardDeleteAsync(staffId);
            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(new { message = result.Message });

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(new { message = result.Message });

            return StatusCode(500, result.Message);
        }


    }
}
