using DakLakCoffeeSupplyChain.Common.DTOs.BusinessStaffDTOs;
using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusinessStaffsController : ControllerBase
    {
        private readonly IBusinessStaffService _businessStaffService;

        public BusinessStaffsController(IBusinessStaffService businessStaffService)
            => _businessStaffService = businessStaffService;

        [HttpPost]
        [Authorize(Roles = "Admin,BusinessManager")]
        public async Task<IActionResult> CreateBusinessStaffAsync([FromBody] BusinessStaffCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _businessStaffService.Create(dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
            {
                return CreatedAtAction(nameof(GetById), new { staffId = result.Data }, new
                {
                    message = result.Message,
                    staffId = result.Data
                });
            }

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(new { message = result.Message });

            return StatusCode(500, new { message = result.Message });
        }

        // Stub for GetById
        [HttpGet("{staffId}")]
        public async Task<IActionResult> GetById(Guid staffId)
        {
            return StatusCode(501); // Chưa triển khai
        }
    }
}
