using DakLakCoffeeSupplyChain.Common.DTOs.ProgressDeviationAnalysisDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProgressDeviationAnalysisController : ControllerBase
    {
        private readonly IProgressDeviationAnalysisService _deviationAnalysisService;

        public ProgressDeviationAnalysisController(IProgressDeviationAnalysisService deviationAnalysisService)
        {
            _deviationAnalysisService = deviationAnalysisService ?? throw new ArgumentNullException(nameof(deviationAnalysisService));
        }

        /// <summary>
        /// Phân tích sai lệch tiến độ cho một mùa vụ cụ thể
        /// </summary>
        [HttpGet("crop-season/{cropSeasonId}")]
        public async Task<IActionResult> AnalyzeCropSeasonDeviation(Guid cropSeasonId)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isManager = IsManager();

                var result = await _deviationAnalysisService.AnalyzeCropSeasonDeviationAsync(
                    cropSeasonId, userId, isAdmin, isManager);

                if (result.Status > 0)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Phân tích sai lệch tiến độ cho một vùng trồng cụ thể
        /// </summary>
        [HttpGet("crop-season-detail/{cropSeasonDetailId}")]
        public async Task<IActionResult> AnalyzeCropSeasonDetailDeviation(Guid cropSeasonDetailId)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isManager = IsManager();

                var result = await _deviationAnalysisService.AnalyzeCropSeasonDetailDeviationAsync(
                    cropSeasonDetailId, userId, isAdmin, isManager);

                if (result.Status > 0)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Phân tích sai lệch tiến độ tổng hợp cho nông dân
        /// </summary>
        [HttpGet("farmer/overall")]
        public async Task<IActionResult> AnalyzeFarmerOverallDeviation()
        {
            try
            {
                var userId = GetUserId();
                var result = await _deviationAnalysisService.AnalyzeFarmerOverallDeviationAsync(userId);

                if (result.Status > 0)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Phân tích sai lệch tiến độ tổng hợp cho hệ thống (Admin/Manager)
        /// </summary>
        [HttpGet("system/overall")]
        [Authorize(Roles = "Admin,Manager,manager,BusinessManager")]
        public async Task<IActionResult> AnalyzeSystemOverallDeviation()
        {
            try
            {
                var isAdmin = IsAdmin();
                var isManager = IsManager();

                var result = await _deviationAnalysisService.AnalyzeSystemOverallDeviationAsync(isAdmin, isManager);

                if (result.Status > 0)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tạo báo cáo sai lệch tiến độ định kỳ
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> GenerateDeviationReport(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] Guid? farmerId = null)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isManager = IsManager();

                // Nếu không phải admin/manager, chỉ có thể xem báo cáo của mình
                if (!isAdmin && !isManager)
                {
                    farmerId = userId;
                }

                var result = await _deviationAnalysisService.GenerateDeviationReportAsync(
                    fromDate, toDate, farmerId, isAdmin, isManager);

                if (result.Status > 0)
                    return Ok(result);
                else
                    return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        #region Helper Methods

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new InvalidOperationException("Không thể xác định người dùng");

            return userId;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") || User.IsInRole("admin");
        }

        private bool IsManager()
        {
            return User.IsInRole("Manager") || User.IsInRole("manager") || User.IsInRole("BusinessManager");
        }

        #endregion
    }
}
