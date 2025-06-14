﻿using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcurementPlansController : ControllerBase
    {
        private readonly IProcurementPlanService _procurementPlanService;

        public ProcurementPlansController(IProcurementPlanService procurementPlanService)
            => _procurementPlanService = procurementPlanService;

        // GET: api/<ProcurementPlans>
        [HttpGet]
        public async Task<IActionResult> GetAllProcurementPlansAsync()
        {
            var result = await _procurementPlanService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET: api/<ProcurementPlans/Available>
        [HttpGet("Available")]
        public async Task<IActionResult> GetAllProcurementPlansAvailableAsync()
        {
            var result = await _procurementPlanService.GetAllProcurementPlansAvailable();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // GET api/<ProcurementPlans>/{planId}
        [HttpGet("{planId}")]
        public async Task<IActionResult> GetById(Guid planId)
        {
            var result = await _procurementPlanService.GetById(planId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }
        // DELETE api/<ProcurementPlans>/{planId}
        [HttpDelete("{planId}")]
        public async Task<IActionResult> DeleteProcurementPlanByIdAsync(Guid planId)
        {
            var result = await _procurementPlanService.DeleteById(planId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy kế hoạch.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
