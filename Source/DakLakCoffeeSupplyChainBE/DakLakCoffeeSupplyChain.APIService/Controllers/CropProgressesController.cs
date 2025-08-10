using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,BusinessManager,Farmer,Expert")]
    public class CropProgressesController : ControllerBase
    {
        private readonly ICropProgressService _cropProgressService;

        public CropProgressesController(ICropProgressService cropProgressService)
        {
            _cropProgressService = cropProgressService;
        }

        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetAllCropProgressesAsync()
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .GetAll(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("by-detail/{cropSeasonDetailId}")]
        public async Task<IActionResult> GetByCropSeasonDetailId(Guid cropSeasonDetailId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .GetByCropSeasonDetailId(cropSeasonDetailId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CropProgressCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .Create(dto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Created(string.Empty, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPut("{progressId}")]
        public async Task<IActionResult> Update(
            Guid progressId, 
            [FromBody] CropProgressUpdateDto dto)
        {
            if (progressId != dto.ProgressId)
                return BadRequest("ProgressId trong route không khớp với nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .Update(dto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return NotFound(result.Message);
        }

        [HttpPatch("soft-delete/{progressId}")]
        public async Task<IActionResult> SoftDeleteById(Guid progressId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .SoftDeleteById(progressId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return NotFound(result.Message);
        }

        [HttpDelete("hard/{progressId}")]
        public async Task<IActionResult> HardDelete(Guid progressId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác định được userId từ token."); }

            var result = await _cropProgressService
                .DeleteById(progressId, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return NotFound(result.Message);
        }
    }
}