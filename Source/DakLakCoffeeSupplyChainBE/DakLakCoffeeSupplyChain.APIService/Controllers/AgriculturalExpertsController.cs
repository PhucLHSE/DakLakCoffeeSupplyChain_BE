using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.AgriculturalExpertDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgriculturalExpertsController : ControllerBase
    {
        private readonly IAgriculturalExpertService _agriculturalExpertService;

        public AgriculturalExpertsController(IAgriculturalExpertService agriculturalExpertService)
        {
            _agriculturalExpertService = agriculturalExpertService;
        }

        // GET: api/agriculturalexperts
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin,AgriculturalExpert,BusinessManager,Farmer")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _agriculturalExpertService
                .GetAllAsync();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/agriculturalexperts/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,AgriculturalExpert,BusinessManager,Farmer")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _agriculturalExpertService
                .GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/agriculturalexperts/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin,AgriculturalExpert")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var result = await _agriculturalExpertService
                .GetByUserIdAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/agriculturalexperts/verified
        [HttpGet("verified")]
        [Authorize(Roles = "Admin,AgriculturalExpert,BusinessManager,Farmer")]
        public async Task<IActionResult> GetVerifiedExperts()
        {
            var result = await _agriculturalExpertService
                .GetVerifiedExpertsAsync();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // PATCH: api/agriculturalexperts/{id}/verify
        [HttpPatch("{id}/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyExpert(Guid id, [FromBody] bool isVerified)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy UserID của admin từ token
            var adminUserId = User.GetUserId();
            if (adminUserId == Guid.Empty)
                return Unauthorized("Không thể xác định người dùng.");

            var result = await _agriculturalExpertService
                .VerifyExpertAsync(id, isVerified, adminUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // POST: api/agriculturalexperts
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] AgriculturalExpertCreateDto createDto, 
            [FromQuery] Guid userId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _agriculturalExpertService
                .CreateAsync(createDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), 
                    new { id = ((AgriculturalExpertViewDetailDto)result.Data).ExpertId }, 
                    result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/agriculturalexperts/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,AgriculturalExpert")]
        public async Task<IActionResult> Update(
            Guid id, 
            [FromBody] AgriculturalExpertUpdateDto updateDto)
        {
            // So sánh route id với dto id để đảm bảo tính nhất quán
            if (id != updateDto.ExpertId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;
            string userRole;

            try
            {
                // Lấy userId và userRole từ token qua ClaimsHelper
                userId = User.GetUserId();
                userRole = User.GetRole();
            }
            catch
            {
                return Unauthorized("Không xác định được userId hoặc role từ token.");
            }

            var result = await _agriculturalExpertService
                .UpdateAsync(updateDto, userId, userRole);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy chuyên gia để cập nhật.");

            return StatusCode(500, result.Message);
        }

        // DELETE: api/agriculturalexperts/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _agriculturalExpertService
                .DeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy chuyên gia.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/agriculturalexperts/soft-delete/{id}
        [HttpPatch("soft-delete/{id}")]
        [Authorize(Roles = "Admin,AgriculturalExpert")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            Guid userId;
            string userRole;

            try
            {
                userId = User.GetUserId();
                userRole = User.GetRole();
            }
            catch
            {
                return Unauthorized("Không xác định được userId hoặc role từ token.");
            }

            var result = await _agriculturalExpertService
                .SoftDeleteAsync(id, userId, userRole);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy chuyên gia.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
