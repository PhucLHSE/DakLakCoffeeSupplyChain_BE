using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CropSeasonsController : ControllerBase
    {
        private readonly ICropSeasonService _cropSeasonService;

        public CropSeasonsController(ICropSeasonService cropSeasonService)
        {
            _cropSeasonService = cropSeasonService;
        }

        // GET: api/CropSeasons
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllCropSeasonsAsync()
        {
            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _cropSeasonService.GetAllByUserId(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/CropSeasons/{id}
        [HttpGet("{cropSeasonId}")]
        public async Task<IActionResult> GetById(Guid cropSeasonId)
        {
            var result = await _cropSeasonService.GetById(cropSeasonId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CropSeasonCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;

            try
            {
                userId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _cropSeasonService.Create(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return CreatedAtAction(nameof(GetById),
                    new { cropSeasonId = ((CropSeasonViewDetailsDto)result.Data).CropSeasonId }, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPut("{cropSeasonId}")]
        public async Task<IActionResult> Update(Guid cropSeasonId, [FromBody] CropSeasonUpdateDto dto)
        {
            if (cropSeasonId != dto.CropSeasonId)
                return BadRequest("Id không khớp.");

            var result = await _cropSeasonService.Update(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return NotFound(result.Message);
        }

        [HttpDelete("{cropSeasonId}")]
        public async Task<IActionResult> DeleteCropSeason(Guid cropSeasonId)
        {
            var result = await _cropSeasonService.DeleteById(cropSeasonId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPatch("soft-delete/{cropSeasonId}")]
        public async Task<IActionResult> SoftDeleteCropSeason(Guid cropSeasonId)
        {
            var result = await _cropSeasonService.SoftDeleteAsync(cropSeasonId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
