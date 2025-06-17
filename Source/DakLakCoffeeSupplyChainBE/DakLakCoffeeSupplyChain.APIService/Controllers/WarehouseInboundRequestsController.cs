using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseInboundRequestDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
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
        [HttpGet]
        [Authorize(Roles = "BusinessStaff,Administrator")]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return StatusCode(result.Status, result);
        }
        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff,Administrator,Farmer")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode(result.Status, result);
        }
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CancelRequest(Guid id)
        {
            var farmerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.CancelRequestAsync(id, farmerId);
            return StatusCode(result.Status, result);
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> RejectRequest(Guid id)
        {
            var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.RejectRequestAsync(id, staffId);
            return StatusCode(result.Status, result);
        }








    }
}
