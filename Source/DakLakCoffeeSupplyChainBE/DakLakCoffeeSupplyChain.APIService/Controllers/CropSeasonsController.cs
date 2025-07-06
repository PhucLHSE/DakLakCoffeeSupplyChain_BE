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
    public class CropSeasonsController : ControllerBase
    {
        private readonly ICropSeasonService _cropSeasonService;

        public CropSeasonsController(ICropSeasonService cropSeasonService)
        {
            _cropSeasonService = cropSeasonService;
        }

        [HttpGet]
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

            // Tách vai trò
            bool isAdmin = role == "Admin";
            bool isManager = role == "BusinessManager";

            // Gọi Service với 2 cờ rõ ràng
            var result = await _cropSeasonService.GetAllByUserId(userId, isAdmin, isManager);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }



        [HttpGet("{cropSeasonId}")]
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
        public async Task<IActionResult> Create([FromBody] CropSeasonCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropSeasonService.Create(dto, userId);
            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById), new { cropSeasonId = ((CropSeasonViewDetailsDto)result.Data).CropSeasonId }, result.Data);
            if (result.Status == Const.FAIL_CREATE_CODE) return Conflict(result.Message);
            return StatusCode(500, result.Message);
        }

        [HttpPut("{cropSeasonId}")]
        public async Task<IActionResult> Update(Guid cropSeasonId, [FromBody] CropSeasonUpdateDto dto)
        {
            if (cropSeasonId != dto.CropSeasonId) return BadRequest("Id không khớp.");

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropSeasonService.Update(dto, userId);
            if (result.Status == Const.SUCCESS_UPDATE_CODE) return Ok(result.Message);
            if (result.Status == Const.FAIL_UPDATE_CODE) return Conflict(result.Message);
            return NotFound(result.Message);
        }
        [HttpDelete("{cropSeasonId}")]
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

            return BadRequest(result.Message); // ❗ Dùng BadRequest thay vì NotFound nếu là lỗi quyền
        }


        [HttpPatch("soft-delete/{cropSeasonId}")]
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

            return BadRequest(result.Message); // Dùng BadRequest thay vì NotFound
        }

    }
}
