using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDTOs;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetAllCropSeasonsAsync()
        {
            var result = await _cropSeasonService.GetAll();

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data); // Trả về danh sách mùa vụ

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); // Không có dữ liệu

            return StatusCode(500, result.Message); // Lỗi hệ thống
        }

        // GET: api/CropSeasons/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCropSeasonByIdAsync(Guid id)
        {
            var result = await _cropSeasonService.GetById(id);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data); // Trả về mùa vụ chi tiết

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message); // Không tìm thấy

            return StatusCode(500, result.Message); // Lỗi hệ thống
        }
    }
}
