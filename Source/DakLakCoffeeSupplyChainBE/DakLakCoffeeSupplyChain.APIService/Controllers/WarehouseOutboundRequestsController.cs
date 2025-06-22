using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseOutboundRequestsController : Controller
    {
        private readonly IWarehouseOutboundRequestService _requestService;

        public WarehouseOutboundRequestsController(IWarehouseOutboundRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpPost]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CreateRequest([FromBody] WarehouseOutboundRequestCreateDto dto)
        {
            var managerUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _requestService.CreateRequestAsync(managerUserId, dto);
            return StatusCode(result.Status, result);
        }
        [HttpGet("{outboundRequestId}")]
        public async Task<IActionResult> GetDetail(Guid outboundRequestId)
        {
            var result = await _requestService.GetDetailAsync(outboundRequestId);
            return StatusCode(result.Status, result);
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _requestService.GetAllAsync();
            return StatusCode(result.Status, result);
        }
    }
}
