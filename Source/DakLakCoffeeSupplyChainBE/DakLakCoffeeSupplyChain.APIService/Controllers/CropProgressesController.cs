using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropProgressDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

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

        // GET: api/CropProgresses
        [HttpGet]
        public async Task<IActionResult> GetAllCropProgressesAsync()
        {
            var result = await _cropProgressService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        // GET: api/CropProgresses/{id}
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
    }
}