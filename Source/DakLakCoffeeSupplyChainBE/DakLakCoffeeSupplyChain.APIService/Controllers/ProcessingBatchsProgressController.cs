using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingBatchsProgressDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
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
    }
}
