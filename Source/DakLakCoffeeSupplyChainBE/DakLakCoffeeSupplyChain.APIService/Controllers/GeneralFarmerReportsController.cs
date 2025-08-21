using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.GeneralFarmerReportDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

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
        public async Task<IActionResult> CreateAsync()
        {
            GeneralFarmerReportCreateDto dto;
            
            // Kiểm tra content type để xử lý phù hợp
            if (Request.ContentType?.Contains("multipart/form-data") == true)
            {
                // Xử lý FormData (có file upload)
                dto = new GeneralFarmerReportCreateDto();
                
                // Parse các trường cơ bản từ FormData
                if (Request.Form.ContainsKey("reportType"))
                    dto.ReportType = Enum.Parse<DakLakCoffeeSupplyChain.Common.Enum.GeneralReportEnums.ReportTypeEnum>(Request.Form["reportType"].ToString());
                
                if (Request.Form.ContainsKey("title"))
                    dto.Title = Request.Form["title"].ToString();
                
                if (Request.Form.ContainsKey("description"))
                    dto.Description = Request.Form["description"].ToString();
                
                if (Request.Form.ContainsKey("severityLevel"))
                    dto.SeverityLevel = Enum.Parse<DakLakCoffeeSupplyChain.Common.Enum.GeneralReportEnums.SeverityLevel>(Request.Form["severityLevel"].ToString());
                
                if (Request.Form.ContainsKey("cropProgressId") && Guid.TryParse(Request.Form["cropProgressId"], out var cropId))
                    dto.CropProgressId = cropId;
                
                if (Request.Form.ContainsKey("processingProgressId") && Guid.TryParse(Request.Form["processingProgressId"], out var procId))
                    dto.ProcessingProgressId = procId;
                
                // Xử lý files
                if (Request.Form.Files.Any(f => f.Name == "photoFiles"))
                    dto.PhotoFiles = Request.Form.Files.Where(f => f.Name == "photoFiles").ToList();
                
                if (Request.Form.Files.Any(f => f.Name == "videoFiles"))
                    dto.VideoFiles = Request.Form.Files.Where(f => f.Name == "videoFiles").ToList();
            }
            else
            {
                // Xử lý JSON (không có file)
                try
                {
                    using var reader = new StreamReader(Request.Body);
                    var jsonBody = await reader.ReadToEndAsync();
                    dto = JsonSerializer.Deserialize<GeneralFarmerReportCreateDto>(jsonBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch
                {
                    return BadRequest("Dữ liệu JSON không hợp lệ.");
                }
            }
            
            if (dto == null)
                return BadRequest("Dữ liệu không hợp lệ.");

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
