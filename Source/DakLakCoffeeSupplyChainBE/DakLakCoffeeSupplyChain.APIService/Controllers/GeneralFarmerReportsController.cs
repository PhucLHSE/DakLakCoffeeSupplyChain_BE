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
    [Authorize(Roles = "Farmer")]
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
            var result = await _reportService.GetAll();
            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);
            return StatusCode(500, result.Message);
        }

        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetById(Guid reportId)
        {
            var result = await _reportService.GetById(reportId);
            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);
            return StatusCode(500, result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] GeneralFarmerReportCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _reportService.CreateGeneralFarmerReports(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                var data = (GeneralFarmerReportViewDetailsDto)result.Data!;
                return CreatedAtAction(nameof(GetById), new { reportId = data.ReportId }, data);
            }

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
