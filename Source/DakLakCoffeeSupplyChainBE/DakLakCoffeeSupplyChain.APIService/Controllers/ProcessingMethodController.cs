using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
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
            var result = await _procesingMethodService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả đúng dữ liệu

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 + message

            return StatusCode(500, result.Message);  // Trả 500 + message
        }

        [HttpGet("{methodId}")]
        [Authorize(Roles = "Admin,Farmer,BusinessManager")]
        public async Task<IActionResult> GetById(int methodId)
        {
            var result = await _procesingMethodService.GetById(methodId);
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
            var result = await _procesingMethodService.DeleteById(methodId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_DELETE_CODE || result.Status == Const.WARNING_NO_DATA_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
