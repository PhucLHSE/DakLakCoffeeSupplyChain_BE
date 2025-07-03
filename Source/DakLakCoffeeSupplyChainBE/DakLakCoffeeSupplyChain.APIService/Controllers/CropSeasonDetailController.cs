using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.CropSeasonDetailDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace DakLakCoffeeSupplyChain.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CropSeasonDetailsController : ControllerBase
    {
        private readonly ICropSeasonDetailService _cropSeasonDetailService;

        public CropSeasonDetailsController(ICropSeasonDetailService cropSeasonDetailService)
        {
            _cropSeasonDetailService = cropSeasonDetailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác thực được người dùng."); }

            // Kiểm tra trực tiếp tên role (không dùng RoleNames)
            bool isAdmin = User.IsInRole("Admin");

            var result = await _cropSeasonDetailService.GetAll(userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE) return Ok(result.Data);
            if (result.Status == Const.WARNING_NO_DATA_CODE) return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }


        // GET: api/CropSeasonDetails/{detailId}
        [HttpGet("{detailId}")]
        public async Task<IActionResult> GetDetail(Guid detailId)
        {
            Guid userId;
            try { userId = User.GetUserId(); }
            catch { return Unauthorized("Không xác thực được người dùng."); }

            bool isAdmin = User.IsInRole("Admin");

            var result = await _cropSeasonDetailService.GetById(detailId, userId, isAdmin);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return Forbid(result.Message);

            return StatusCode(500, result.Message);
        }


        // POST: api/CropSeasonDetails
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] CropSeasonDetailCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cropSeasonDetailService.Create(dto);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return StatusCode(201, result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        // PUT: api/CropSeasonDetails/{detailId}
        [HttpPut("{detailId}")]
        public async Task<IActionResult> UpdateAsync(Guid detailId, [FromBody] CropSeasonDetailUpdateDto dto)
        {
            if (detailId != dto.DetailId)
                return BadRequest("ID trong route không khớp với ID trong nội dung.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _cropSeasonDetailService.Update(dto);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return Conflict(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy dòng mùa vụ.");

            return StatusCode(500, result.Message);
        }

        // DELETE: api/CropSeasonDetails/{detailId}
        [HttpDelete("{detailId}")]
        public async Task<IActionResult> DeleteByIdAsync(Guid detailId)
        {
            var result = await _cropSeasonDetailService.DeleteById(detailId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy dòng mùa vụ.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa thất bại.");

            return StatusCode(500, result.Message);
        }

        // PATCH: api/CropSeasonDetails/soft-delete/{detailId}
        [HttpPatch("soft-delete/{detailId}")]
        public async Task<IActionResult> SoftDeleteAsync(Guid detailId)
        {
            var result = await _cropSeasonDetailService.SoftDeleteById(detailId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok("Xóa mềm thành công.");

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound("Không tìm thấy dòng mùa vụ.");

            if (result.Status == Const.FAIL_DELETE_CODE)
                return Conflict("Xóa mềm thất bại.");

            return StatusCode(500, result.Message);
        }
    }
}
