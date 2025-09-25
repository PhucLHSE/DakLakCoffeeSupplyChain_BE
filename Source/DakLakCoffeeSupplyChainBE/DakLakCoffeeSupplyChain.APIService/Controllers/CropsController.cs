using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using DakLakCoffeeSupplyChain.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CropsController : ControllerBase
    {
        private readonly ICropService _cropService;
        private const string ERROR_USER_ID_NOT_FOUND_MSG = "Không xác định được userId từ token.";

        public CropsController(ICropService cropService)
            => _cropService = cropService;

        // GET: api/<CropsController>
        [HttpGet]
        [EnableQuery]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> GetAllCropsAsync()
        {
            Guid userId;

            try
            {
                // Lấy userId từ token qua ClaimsHelper
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService
                .GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/<CropsController>/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> GetCropByIdAsync(Guid id)
        {
            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.GetById(id, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // POST: api/<CropsController>
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> CreateCropAsync([FromBody] CropCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.Create(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/<CropsController>/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> UpdateCropAsync(Guid id, [FromBody] CropUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != dto.CropId)
            {
                return BadRequest("ID trong URL không khớp với ID trong body.");
            }

            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.Update(dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        // DELETE: api/<CropsController>/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> DeleteCropAsync(Guid id)
        {
            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized(ERROR_USER_ID_NOT_FOUND_MSG);
            }

            var result = await _cropService.Delete(id, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return NoContent();

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.ERROR_EXCEPTION)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
