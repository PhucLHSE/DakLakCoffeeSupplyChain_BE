using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseInboundRequestsController : ControllerBase
    {
        private readonly IWarehouseInboundRequestService _service;

        public WarehouseInboundRequestsController(IWarehouseInboundRequestService service)
        {
            _service = service;
        }

        /// <summary>Farmer gửi yêu cầu nhập kho</summary>
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CreateInboundRequest([FromBody] WarehouseInboundRequestCreateDto dto)
        {
            var farmerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.CreateRequestAsync(farmerId, dto);
            return StatusCode(result.Status, result);
        }
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> ApproveRequest(Guid id)
        {
            var staffUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.ApproveRequestAsync(id, staffUserId);
            return StatusCode(result.Status, result);
        }







    }
}
