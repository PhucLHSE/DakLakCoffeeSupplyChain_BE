using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingBatchsProgressController : ControllerBase
    {
        private readonly IProcessingBatchProgressService _processingBatchProgressService;

        public ProcessingBatchsProgressController(IProcessingBatchProgressService processingBatchProgressService)
        {
            _processingBatchProgressService = processingBatchProgressService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var result = await _processingBatchProgressService.GetAllAsync();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _processingBatchProgressService.GetByIdAsync(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProcessingBatchProgressCreateDto input)
        {
            var result = await _processingBatchProgressService.CreateAsync(input);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(StatusCodes.Status201Created, result.Message);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProcessingBatchProgressUpdateDto input)
        {
            var result = await _processingBatchProgressService.UpdateAsync(id, input);

            // ✅ Trường hợp thành công: chỉ trả về .Data
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data ?? new { });

            // ❌ Trường hợp lỗi: trả về full ServiceResult (message, status, errors)
            if (result.Status == Const.FAIL_UPDATE_CODE || result.Status == Const.ERROR_VALIDATION_CODE)
                return BadRequest(result);

            return StatusCode(500, result); // fallback lỗi hệ thống
        }
        [HttpDelete("soft/{id}")]
        // [Authorize(Roles = "BusinessStaff,Farmer")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var result = await _processingBatchProgressService.SoftDeleteAsync(id);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
