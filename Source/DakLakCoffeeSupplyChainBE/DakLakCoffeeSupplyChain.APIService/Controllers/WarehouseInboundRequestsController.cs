using DakLakCoffeeSupplyChain.Common;
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
            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result);

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result);

            return StatusCode(500, result); // fallback
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> ApproveRequest(Guid id)
        {
            var staffUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.ApproveRequestAsync(id, staffUserId);
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result);

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result);

            return StatusCode(500, result); // fallback
        }

        [HttpGet]
        [Authorize(Roles = "BusinessStaff,Administrator")]
        [EnableQuery]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result);

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result);

            return StatusCode(500, result); // fallback
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessStaff,Administrator,Farmer")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result);

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result);

            return StatusCode(500, result); // fallback
        }

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CancelRequest(Guid id)
        {
            var farmerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.CancelRequestAsync(id, farmerId);
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result);

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result);

            return StatusCode(500, result); // fallback
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "BusinessStaff")]
        public async Task<IActionResult> RejectRequest(Guid id)
        {
            var staffId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _service.RejectRequestAsync(id, staffId);
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result);

            if (result.Status == Const.ERROR_EXCEPTION)
                return StatusCode(500, result);

            return StatusCode(500, result); // fallback
        }









    }
}
