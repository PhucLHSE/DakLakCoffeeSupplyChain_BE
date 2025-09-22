using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.WalletTransactionDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DakLakCoffeeSupplyChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletTransactionController : ControllerBase
    {
        private readonly IWalletTransactionService _walletTransactionService;

        public WalletTransactionController(IWalletTransactionService walletTransactionService)
        {
            _walletTransactionService = walletTransactionService;
        }

        /// <summary>
        /// Tạo giao dịch ví mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer,Admin")]
        public async Task<IActionResult> CreateWalletTransactionAsync([FromBody] WalletTransactionCreateDto createDto)
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

            var result = await _walletTransactionService.CreateAsync(createDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Created("", result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy chi tiết giao dịch theo ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer,Admin")]
        public async Task<IActionResult> GetWalletTransactionByIdAsync(Guid id)
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

            var result = await _walletTransactionService.GetByIdAsync(id, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy lịch sử giao dịch theo ví
        /// </summary>
        [HttpGet("wallet/{walletId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer,Admin")]
        public async Task<IActionResult> GetTransactionsByWalletAsync(Guid walletId)
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

            var result = await _walletTransactionService.GetByWalletIdAsync(walletId, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

 

        /// <summary>
        /// Lấy giao dịch theo User ID (tất cả role đều có thể dùng)
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer,Admin")]
        public async Task<IActionResult> GetTransactionsByUserIdAsync(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            Guid currentUserId;
            try
            {
                currentUserId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _walletTransactionService.GetTransactionsByUserIdAsync(userId, pageNumber, pageSize, currentUserId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy giao dịch System Wallet (chỉ Admin)
        /// </summary>
        [HttpGet("system-wallet")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWalletTransactionsAsync([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            Guid currentUserId;
            try
            {
                currentUserId = User.GetUserId();
            }
            catch
            {
                return Unauthorized("Không xác định được userId từ token.");
            }

            var result = await _walletTransactionService.GetSystemWalletTransactionsAsync(pageNumber, pageSize, currentUserId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Cập nhật giao dịch (chỉ cho phép cập nhật description)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Admin")]
        public async Task<IActionResult> UpdateWalletTransactionAsync(Guid id, [FromBody] WalletTransactionUpdateDto updateDto)
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

            var result = await _walletTransactionService.UpdateAsync(id, updateDto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Xóa giao dịch (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Admin")]
        public async Task<IActionResult> DeleteWalletTransactionAsync(Guid id)
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

            var result = await _walletTransactionService.DeleteAsync(id, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_DELETE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Xóa vĩnh viễn giao dịch (chỉ Admin)
        /// </summary>
        [HttpDelete("{id}/hard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDeleteWalletTransactionAsync(Guid id)
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

            var result = await _walletTransactionService.HardDeleteAsync(id, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_DELETE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }
    }
}
