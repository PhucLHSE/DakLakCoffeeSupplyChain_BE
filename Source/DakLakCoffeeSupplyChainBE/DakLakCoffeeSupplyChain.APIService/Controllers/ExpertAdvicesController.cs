using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ExpertAdviceDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.Base;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "AgriculturalExpert,Admin")] // ✅ Áp dụng xác thực mặc định
    public class ExpertAdvicesController : ControllerBase
    {
        private readonly IExpertAdviceService _expertAdviceService;

        public ExpertAdvicesController(IExpertAdviceService expertAdviceService)
            => _expertAdviceService = expertAdviceService;

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllExpertAdvicesAsync()
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
                return Unauthorized("Không xác định được người dùng.");
            }

            bool isAdmin = role == "Admin";

            var result = await _expertAdviceService
                .GetAllByUserIdAsync(userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("{adviceId}", Name = "GetExpertAdviceById")]
        public async Task<IActionResult> GetExpertAdviceByIdAsync(Guid adviceId)
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
                return Unauthorized("Không xác định được người dùng.");
            }

            bool isAdmin = role == "Admin";

            var result = await _expertAdviceService
                .GetByIdAsync(adviceId, userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE || result.Status == Const.FAIL_READ_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost]
        [Authorize(Roles = "AgriculturalExpert")]
        public async Task<IActionResult> CreateExpertAdviceAsync(
            [FromBody] ExpertAdviceCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ServiceResult(
                    Const.ERROR_VALIDATION_CODE,
                    "Dữ liệu không hợp lệ",
                    errors
                ));
            }

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _expertAdviceService
                .CreateAsync(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                var response = (ExpertAdviceViewDetailDto)result.Data;
                return CreatedAtAction("GetExpertAdviceById",
                    new { adviceId = response.AdviceId },
                    response);

            }

            if (result.Status == Const.WARNING_NO_DATA_CODE || result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpPut("{adviceId}")]
        [Authorize(Roles = "AgriculturalExpert")]
        public async Task<IActionResult> UpdateExpertAdviceAsync(
            Guid adviceId, 
            [FromBody] ExpertAdviceUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ServiceResult(
                    Const.ERROR_VALIDATION_CODE, 
                    "Dữ liệu không hợp lệ", 
                    errors)
                );
            }

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _expertAdviceService
                .UpdateAsync(adviceId, dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPatch("soft-delete/{adviceId}")]
        [Authorize(Roles = "AgriculturalExpert,Admin")]
        public async Task<IActionResult> SoftDeleteExpertAdviceAsync(Guid adviceId)
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
                return Unauthorized("Không xác định được người dùng.");
            }

            bool isAdmin = role == "Admin";

            var result = await _expertAdviceService
                .SoftDeleteAsync(adviceId, userId, isAdmin);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpDelete("{adviceId}")]
        [Authorize(Roles = "Admin")] // chỉ Admin được xóa vĩnh viễn
        public async Task<IActionResult> HardDeleteExpertAdviceAsync(Guid adviceId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được người dùng."); }

            var result = await _expertAdviceService
                .HardDeleteAsync(adviceId, userId, isAdmin: true);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
