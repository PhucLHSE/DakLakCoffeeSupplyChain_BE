using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.ProcessingParameterDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcessingParametersController : ControllerBase
    {
        private readonly IProcessingParameterService _processingParameterService;

        public ProcessingParametersController(IProcessingParameterService processingParameterService)
        {
            _processingParameterService = processingParameterService;
        }

        [HttpGet]
        //[Authorize(Roles = "Admin,BusinessManager,AgriculturalExpert,BusinessStaff,Farmer")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _processingParameterService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("{parameterId}")]
        //[Authorize(Roles = "Admin,BusinessManager,BusinessStaff,AgriculturalExpert,Farmer")]
        public async Task<IActionResult> GetById(Guid parameterId)
        {
            var result = await _processingParameterService.GetById(parameterId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);              // Trả object chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);     // Trả 404 nếu không tìm thấy

            return StatusCode(500, result.Message);  // Lỗi hệ thống
        }

        [HttpPost]
        //[Authorize(Roles = "BusinessStaff,Farmer")]
        public async Task<IActionResult> Create([FromBody] ProcessingParameterCreateDto dto)
        {
            var result = await _processingParameterService.CreateAsync(dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data); // 200 OK

            if (result.Status == Const.ERROR_VALIDATION_CODE)
                return BadRequest(result.Message); // 400 Bad Request nếu validate fail

            return StatusCode(500, result.Message); // 500 Internal Server Error nếu exception
        }

    }
}
