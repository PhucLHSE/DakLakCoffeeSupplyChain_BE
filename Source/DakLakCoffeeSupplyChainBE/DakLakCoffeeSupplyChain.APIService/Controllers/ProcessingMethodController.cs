using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingMethodDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingMethodController : ControllerBase
    {
        private readonly IProcessingMethodService _procesingMethodService;

        public ProcessingMethodController(IProcessingMethodService procesingMethodServiceservice)
        {
            _procesingMethodService = procesingMethodServiceservice;
        }

        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Admin,Farmer,BusinessManager")]
        public async Task<IActionResult> GetAllProcesingMethodsAsync()
        {
            var result = await _procesingMethodService
                .GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Farmer,BusinessManager")]
        public async Task<IActionResult> Create(
            [FromBody] ProcessingMethodCreateDto dto)
        {
            var result = await _procesingMethodService
                .CreateAsync(dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Message);  // Trả 200 nếu thành công

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message); // Trả 400 nếu lỗi do input

            return StatusCode(500, result.Message); // Trả 500 nếu exception
        }

        [HttpPut("{methodId}")]
        [Authorize(Roles = "Admin,Farmer,BusinessManager")]
        public async Task<IActionResult> Update(
            int methodId,
            [FromBody] ProcessingMethodUpdateDto dto)
        {
            if (methodId != dto.MethodId)
                return BadRequest("ID trong URL không khớp với dữ liệu gửi lên.");

            var result = await _procesingMethodService
                .UpdateAsync(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_VALIDATION_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("{methodId}")]
        [Authorize(Roles = "Admin,Farmer,BusinessManager")]
        public async Task<IActionResult> GetById(int methodId)
        {
            var result = await _procesingMethodService
                .GetById(methodId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);
        }

        [HttpDelete("{methodId}")]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> DeleteById(int methodId)
        {
            var result = await _procesingMethodService
                .DeleteById(methodId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPatch("{methodId}/soft-delete")]
        [Authorize(Roles = "Admin,BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> SoftDelete(int methodId)
        {
            var result = await _procesingMethodService
                .SoftDeleteAsync(methodId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
