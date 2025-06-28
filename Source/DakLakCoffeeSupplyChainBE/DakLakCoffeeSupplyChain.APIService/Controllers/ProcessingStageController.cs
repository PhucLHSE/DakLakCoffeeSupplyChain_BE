using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodStageDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingStagesController : ControllerBase
    {
        private readonly IProcessingStageService _processingStageService;

        public ProcessingStagesController(IProcessingStageService processingStageService)
        {
            _processingStageService = processingStageService;
        }

   
        [HttpGet]
        [EnableQuery]
        //[Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _processingStageService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }


        [HttpGet("detail/{stageId}")]
        //[Authorize(Roles = "Admin,BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetDetail(int stageId)
        {
            var result = await _processingStageService.GetDetailByIdAsync(stageId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); 

            return StatusCode(500, result.Message); 
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,BusinessManager,BusinessStaff")]
        public async Task<IActionResult> Create([FromBody] CreateProcessingStageDto dto)
        {
            var result = await _processingStageService.CreateAsync(dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data); // hoặc return Created(...) nếu cần RESTful hơn

            if (result.Status == Const.WARNING_NO_DATA_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return BadRequest(result);

            return StatusCode(500, result.Message); // lỗi không mong muốn
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _processingStageService.DeleteAsync(id); 
            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "Admin,BusinessManager,BusinessStaff")]
        public async Task<IActionResult> Update(int id, [FromBody] ProcessingStageUpdateDto dto)
        {
            if (id != dto.StageId)
                return BadRequest("ID trong URL không khớp với dữ liệu gửi lên.");

            var result = await _processingStageService.UpdateAsync(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }

}

