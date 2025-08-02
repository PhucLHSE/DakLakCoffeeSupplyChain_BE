using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Mặc định yêu cầu xác thực cho toàn bộ controller
    public class CropSeasonsController : ControllerBase
    {
        private readonly ICropSeasonService _cropSeasonService;

        public CropSeasonsController(ICropSeasonService cropSeasonService)
        {
            _cropSeasonService = cropSeasonService;
        }

        // ✅ Ai cũng xem được mùa vụ liên quan (Admin, Manager, Farmer)
        [HttpGet]
        [Authorize(Roles = "Admin,BusinessManager,Farmer,Expert")]
        public async Task<IActionResult> GetAllCropSeasonsAsync()
        {
            Guid userId;
            string? role;

            try
            {
                userId = User.GetUserId();
                role = User.FindFirst(ClaimTypes.Role)?.Value;
            }
            catch
            {
                return Unauthorized("Không xác thực được người dùng. Vui lòng đăng nhập.");
            }

            bool isAdmin = role == "Admin";
            bool isManager = role == "BusinessManager";

            var result = await _cropSeasonService.GetAllByUserId(userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // ✅ Ai cũng được quyền xem chi tiết mùa vụ của mình
        [HttpGet("{cropSeasonId}")]
        [Authorize(Roles = "Admin,BusinessManager,Farmer,Expert")]
        public async Task<IActionResult> GetById(Guid cropSeasonId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropSeasonService.GetById(cropSeasonId, userId);
            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE || result.Status == Const.FAIL_UPDATE_CODE)
                return NotFound(result.Message);
            return StatusCode(500, result.Message);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,BusinessManager,Farmer")]
        public async Task<IActionResult> Create([FromBody] CropSeasonCreateDto dto)
        {
            // ✅ Validate model binding
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Lấy UserId từ token
            Guid userId;
            try
            {
                userId = User.GetUserId(); // extension method custom của bạn
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            // ✅ Gọi service xử lý tạo mùa vụ
            var result = await _cropSeasonService.Create(dto, userId);

            // ✅ Nếu tạo thành công: trả về CreatedAtAction
            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                var response = (CropSeasonViewDetailsDto)result.Data;
                return CreatedAtAction(
                    nameof(GetById),                                  // action
                    new { cropSeasonId = response.CropSeasonId },    // route values
                    response                                          // response body
                );
            }

            // ❌ Nếu lỗi logic như cam kết trùng, chưa duyệt, v.v...
            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            // ❗ Nếu lỗi không rõ (exception hoặc lỗi hệ thống)
            return StatusCode(500, result.Message);
        }



        [HttpPut("{cropSeasonId}")]
        [Authorize(Roles = "Admin,BusinessManager,Farmer")]
        public async Task<IActionResult> Update(Guid cropSeasonId, [FromBody] CropSeasonUpdateDto dto)
        {
            if (cropSeasonId != dto.CropSeasonId)
                return BadRequest(new { message = "Id không khớp." });

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized(new { message = "Không xác định được userId từ token." }); }

            var result = await _cropSeasonService.Update(dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(new { message = result.Message });

            return BadRequest(new { message = result.Message });
        }



        // ✅ Chỉ Admin hoặc người tạo được xoá
        [HttpDelete("{cropSeasonId}")]
        [Authorize(Roles = "Admin,BusinessManager,Farmer")]
        public async Task<IActionResult> DeleteCropSeason(Guid cropSeasonId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = role == "Admin";

            var result = await _cropSeasonService.DeleteById(cropSeasonId, userId, isAdmin);
            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return BadRequest(result.Message);
        }

        // ✅ Soft delete - chỉ Admin và người tạo được phép
        [HttpPatch("soft-delete/{cropSeasonId}")]
        [Authorize(Roles = "Admin,BusinessManager,Farmer")] 
        public async Task<IActionResult> SoftDeleteCropSeason(Guid cropSeasonId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            bool isAdmin = role == "Admin";

            var result = await _cropSeasonService.SoftDeleteAsync(cropSeasonId, userId, isAdmin);
            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return BadRequest(result.Message);
        }
    }

}

