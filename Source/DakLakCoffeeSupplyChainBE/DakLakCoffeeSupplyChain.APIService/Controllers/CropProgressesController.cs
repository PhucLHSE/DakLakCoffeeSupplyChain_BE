using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var result = await _cropProgressService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpGet("{progressId}")]
        public async Task<IActionResult> GetById(Guid progressId)
        {
            var result = await _cropProgressService.GetById(progressId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPost]

        public async Task<IActionResult> Create([FromBody] CropProgressCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cropProgressService.Create(dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Created(string.Empty, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        [HttpPut("{progressId}")]

        public async Task<IActionResult> Update(Guid progressId, [FromBody] CropProgressUpdateDto dto)
        {
            if (progressId != dto.ProgressId)
                return BadRequest("ProgressId trong route không khớp với nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cropProgressService.Update(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            return NotFound(result.Message);
        }

        [HttpPatch("soft-delete/{progressId}")]

        public async Task<IActionResult> SoftDeleteById(Guid progressId)
        {
            var result = await _cropProgressService.SoftDeleteById(progressId);
            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return NotFound(result.Message);
        }


        [HttpDelete("hard/{progressId}")]
        public async Task<IActionResult> HardDelete(Guid progressId)
        {
            var result = await _cropProgressService.DeleteById(progressId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            return NotFound(result.Message);
        }

    }
}