using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Common.DTOs.SystemConfigurationDTOs.ProcessingBatchCriteria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemConfigurationController (ISystemConfigurationService service) : ControllerBase
    {
        private readonly ISystemConfigurationService _service = service;

        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAsync()
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _service
                .GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        [HttpGet("{configName}")]
        [EnableQuery]
        public async Task<IActionResult> GetAllAsync(string configName)
        {

            var result = await _service
                .GetByName(configName);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        // ========== API CRUD TIÊU CHÍ ĐÁNH GIÁ CHẤT LƯỢNG PROCESSINGBATCH ==========
        
        [HttpGet("processing-batch/criteria")]
        [EnableQuery]
        [Authorize(Roles = "Admin,AgriculturalExpert")]
        public async Task<IActionResult> GetProcessingBatchCriteriaAsync()
        {
            try
            {
                var result = await _service.GetProcessingBatchCriteriaAsync();
                
                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);
                    
                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpGet("processing-batch/criteria/{name}")]
        [EnableQuery]
        [Authorize(Roles = "Admin,AgriculturalExpert")]
        public async Task<IActionResult> GetProcessingBatchCriteriaByNameAsync(string name)
        {
            try
            {
                var result = await _service.GetProcessingBatchCriteriaByNameAsync(name);
                
                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);
                    
                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpPost("processing-batch/criteria")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProcessingBatchCriteriaAsync([FromBody] CreateProcessingBatchCriteriaDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _service.CreateProcessingBatchCriteriaAsync(dto, userId);
                
                if (result.Status == Const.SUCCESS_CREATE_CODE)
                    return CreatedAtAction(nameof(GetProcessingBatchCriteriaByNameAsync), new { name = dto.Name }, result.Data);
                    
                if (result.Status == Const.FAIL_CREATE_CODE)
                    return BadRequest(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpPut("processing-batch/criteria/{name}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProcessingBatchCriteriaAsync(string name, [FromBody] UpdateProcessingBatchCriteriaDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _service.UpdateProcessingBatchCriteriaAsync(name, dto, userId);
                
                if (result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(result.Data);
                    
                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);
                    
                if (result.Status == Const.FAIL_UPDATE_CODE)
                    return BadRequest(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpDelete("processing-batch/criteria/{name}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProcessingBatchCriteriaAsync(string name)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _service.DeleteProcessingBatchCriteriaAsync(name, userId);
                
                if (result.Status == Const.SUCCESS_DELETE_CODE)
                    return Ok(result.Message);
                    
                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpPatch("processing-batch/criteria/{name}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateProcessingBatchCriteriaAsync(string name)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _service.ActivateProcessingBatchCriteriaAsync(name, userId);
                
                if (result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(result.Message);
                    
                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpPatch("processing-batch/criteria/{name}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateProcessingBatchCriteriaAsync(string name)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _service.DeactivateProcessingBatchCriteriaAsync(name, userId);
                
                if (result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(result.Message);
                    
                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);
                    
                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
    }
}
