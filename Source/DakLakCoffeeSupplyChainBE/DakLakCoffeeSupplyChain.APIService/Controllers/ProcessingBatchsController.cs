using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingBatchController : ControllerBase
    {
        private readonly IProcessingBatchService _processingbatchservice;

        public ProcessingBatchController(IProcessingBatchService processingbatchservice)
        {
            _processingbatchservice = processingbatchservice;
        }

        // GET: api/processing-batch

        [HttpGet]
        [EnableQuery]
        
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var userIdStr = User.FindFirst("userId")?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
            {
                return BadRequest("Không thể lấy userId từ token.");
            }

            var isAdmin = User.IsInRole("Admin");

            var result = await _processingbatchservice.GetAllByUserId(userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

    }
}
