using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingWasteDisposalController : ControllerBase
    {
        private readonly IProcessingWasteDisposalService _disposalService;

        public ProcessingWasteDisposalController(IProcessingWasteDisposalService disposalService)
        {
            _disposalService = disposalService;
        }


        [HttpGet("view-all")]
        [Authorize(Roles = "Farmer,Admin")]
        public async Task<IActionResult> GetAll()
        {
            // Lấy userId từ token
            var userIdStr = User.FindFirst("userId")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Không thể xác định userId từ token.");

            var isAdmin = User.IsInRole("Admin");

            // Gọi Service
            var result = await _disposalService.GetAllAsync(userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return BadRequest(result.Message);
        }
    }
}