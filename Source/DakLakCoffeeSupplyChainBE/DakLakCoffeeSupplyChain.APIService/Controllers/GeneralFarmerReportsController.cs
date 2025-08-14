using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
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
    [Authorize(Roles = "Farmer,AgriculturalExpert,Admin,BusinessManager")]
    public class GeneralFarmerReportsController : ControllerBase
    {
        private readonly IGeneralFarmerReportService _reportService;

        public GeneralFarmerReportsController(IGeneralFarmerReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var result = await _reportService
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // ✅ Thêm endpoint cho BusinessManager xem tất cả báo cáo
        [HttpGet("manager/all")]
        [Authorize(Roles = "BusinessManager,Admin")]
        public async Task<IActionResult> GetAllForManager()
        {
            // Kiểm tra role của user hiện tại
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
            {
                return Unauthorized("Không xác định được role của user.");
            }

            // Chỉ cho phép BusinessManager và Admin
            if (role != "BusinessManager" && role != "Admin")
            {
                return Forbid("Bạn không có quyền truy cập endpoint này.");
            }

            var result = await _reportService
                .GetAllForManagerAsync();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // ✅ Endpoint test để kiểm tra role của user
        [HttpGet("test-role")]
        [Authorize]
        public IActionResult TestRole()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            return Ok(new
            {
                Role = role,
                UserId = userId,
                Email = email,
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }

        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetById(Guid reportId)
        {
            var result = await _reportService
                .GetById(reportId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(
            [FromBody] GeneralFarmerReportCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _reportService
                .CreateGeneralFarmerReports(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                var data = (GeneralFarmerReportViewDetailsDto)result.Data!;

                return CreatedAtAction(nameof(GetById), new { reportId = data.ReportId }, data);
            }

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPut("{reportId}")]
        public async Task<IActionResult> UpdateAsync(
            Guid reportId, 
            [FromBody] GeneralFarmerReportUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (reportId != dto.ReportId)
                return BadRequest("ID trong route không khớp với nội dung.");

            var result = await _reportService
                .UpdateGeneralFarmerReport(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPatch("soft-delete/{reportId}")]
        public async Task<IActionResult> SoftDeleteAsync(Guid reportId)
        {
            var result = await _reportService
                .SoftDeleteGeneralFarmerReport(reportId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy báo cáo cần xóa mềm.");

            return StatusCode(500, result.Message);
        }

        [HttpDelete("{reportId}")]
        public async Task<IActionResult> HardDeleteAsync(Guid reportId)
        {
            var result = await _reportService
                .HardDeleteGeneralFarmerReport(reportId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa cứng thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy báo cáo cần xóa.");

            return StatusCode(500, result.Message);
        }

        [HttpPatch("{reportId}/resolve")]
        [Authorize(Roles = "AgriculturalExpert,Admin")]
        public async Task<IActionResult> ResolveAsync(Guid reportId)
        {
            Guid expertId;
            try { expertId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được expertId từ token."); }

            var result = await _reportService
                .ResolveGeneralFarmerReportAsync(reportId, expertId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
