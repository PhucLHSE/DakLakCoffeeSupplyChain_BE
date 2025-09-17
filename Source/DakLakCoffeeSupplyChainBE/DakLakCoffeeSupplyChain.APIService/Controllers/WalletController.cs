using DakLakCoffeeSupplyChain.Common;
using DakLakCoffeeSupplyChain.Common.DTOs.WalletDTOs;
using DakLakCoffeeSupplyChain.Common.Helpers;
using DakLakCoffeeSupplyChain.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DakLakCoffeeSupplyChain.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Tạo ví cho người dùng hiện tại
        /// </summary>
        // POST: api/Wallets
        [HttpPost]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> CreateWalletAsync(
            [FromBody] WalletCreateDto walletCreateDto)
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

            // Override userId từ request với userId từ token
            walletCreateDto.UserId = userId;

            var result = await _walletService
                .Create(walletCreateDto, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Created("", result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return Conflict(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy danh sách tất cả ví của công ty
        /// </summary>
        // GET: api/Wallets
        [HttpGet]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetAllWalletsAsync()
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

            var result = await _walletService.GetAllAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy chi tiết ví theo ID
        /// </summary>
        // GET: api/Wallets/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetWalletByIdAsync(Guid id)
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

            var result = await _walletService.GetByIdAsync(id, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Cập nhật thông tin ví
        /// </summary>
        // PUT: api/Wallets/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> UpdateWalletAsync(Guid id, [FromBody] WalletUpdateDto walletUpdateDto)
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

            var result = await _walletService.UpdateAsync(id, walletUpdateDto, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_UPDATE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Xóa ví (soft delete)
        /// </summary>
        // DELETE: api/Wallets/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> DeleteWalletAsync(Guid id)
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

            var result = await _walletService.DeleteAsync(id, userId);

            if (result.Status == Const.SUCCESS_DELETE_CODE)
                return Ok(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_DELETE_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy thông tin ví của người dùng hiện tại
        /// </summary>
        // GET: api/Wallets/my-wallet
        [HttpGet("my-wallet")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> GetMyWalletAsync()
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

            var result = await _walletService.GetMyWalletAsync(userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Lấy số dư ví
        /// </summary>
        // GET: api/Wallets/{id}/balance
        [HttpGet("{id}/balance")]
        [Authorize(Roles = "BusinessManager,BusinessStaff")]
        public async Task<IActionResult> GetWalletBalanceAsync(Guid id)
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

            var result = await _walletService.GetWalletBalanceAsync(id, userId);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            if (result.Status == Const.FAIL_READ_CODE)
                return BadRequest(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Tạo giao dịch nạp tiền vào ví
        /// </summary>
        // POST: api/Wallets/topup
        [HttpPost("topup")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> CreateTopupPaymentAsync([FromBody] WalletTopupRequestDto request)
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

            var result = await _walletService.CreateTopupPaymentAsync(request, userId);

            if (result.Status == Const.SUCCESS_CREATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Xử lý kết quả thanh toán nạp tiền
        /// </summary>
        // POST: api/Wallets/process-topup
        [HttpPost("process-topup")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> ProcessTopupPaymentAsync([FromBody] ProcessTopupRequest request)
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

            var result = await _walletService.ProcessTopupPaymentAsync(request.TransactionId, request.Amount, userId);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.SUCCESS_READ_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }

        /// <summary>
        /// Nạp tiền trực tiếp (test mode)
        /// </summary>
        // POST: api/Wallets/direct-topup
        [HttpPost("direct-topup")]
        [Authorize(Roles = "BusinessManager,BusinessStaff,Farmer")]
        public async Task<IActionResult> DirectTopupAsync([FromBody] DirectTopupRequest request)
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

            var result = await _walletService.DirectTopupAsync(userId, request.Amount, request.Description);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result.Data);

            if (result.Status == Const.FAIL_CREATE_CODE)
                return BadRequest(result.Message);

            if (result.Status == Const.WARNING_NO_DATA_CODE)
                return NotFound(result.Message);

            return StatusCode(500, result.Message);
        }
    }

    public class ProcessTopupRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public double Amount { get; set; }
    }

    public class DirectTopupRequest
    {
        public double Amount { get; set; }
        public string? Description { get; set; }
    }
}
