using DakLakCoffeeSupplyChain.Common.DTOs.WarehouseOutboundRequestDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseOutboundRequestsController : ControllerBase
    {
        private readonly IWarehouseOutboundRequestService _requestService;

        public WarehouseOutboundRequestsController(IWarehouseOutboundRequestService requestService)
        {
            _requestService = requestService;
        }

        // POST: api/WarehouseOutboundRequests
        [HttpPost]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> CreateRequest([FromBody] WarehouseOutboundRequestCreateDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid managerUserId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _requestService.CreateRequestAsync(managerUserId, dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetDetail), new { outboundRequestId = result.Data }, result);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result);

            return StatusCode(500, result);
        }

        // GET: api/WarehouseOutboundRequests/all
        [HttpGet("all")]
        [EnableQuery]
        [Authorize(Roles = "BusinessManager, BusinessStaff")]
        public async Task<IActionResult> GetAll()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid managerUserId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _requestService.GetAllAsync(managerUserId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            return StatusCode(500, result);
        }

        // GET: api/WarehouseOutboundRequests/{outboundRequestId}
        [HttpGet("{outboundRequestId}")]
        [Authorize(Roles = "BusinessStaff, BusinessManager")]
        public async Task<IActionResult> GetDetail(Guid outboundRequestId)
        {
            var result = await _requestService.GetDetailAsync(outboundRequestId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result);

            if (result.Status == Const.FAIL_READ_CODE)
                return NotFound(result);

            return StatusCode(500, result);
        }

        // PUT: api/WarehouseOutboundRequests/{id}/accept
        [HttpPut("{id}/accept")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> Accept(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid staffUserId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _requestService.AcceptRequestAsync(id, staffUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result);

            return StatusCode(500, result);
        }

        // PUT: api/WarehouseOutboundRequests/{id}/cancel
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "BusinessManager")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid managerUserId))
                return Unauthorized("Cannot determine user from token.");

            var result = await _requestService.CancelRequestAsync(id, managerUserId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result);

            return StatusCode(500, result);
        }
    }
}
