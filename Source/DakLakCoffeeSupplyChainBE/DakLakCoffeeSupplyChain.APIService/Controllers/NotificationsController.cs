using DakLakCoffeeSupplyChain.Common;
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
    [Authorize] // Yêu cầu đăng nhập
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user")]
        [EnableQuery]
        public async Task<IActionResult> GetUserNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                Guid userId = User.GetUserId();
                var result = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                Guid userId = User.GetUserId();
                var result = await _notificationService.GetUnreadCountAsync(userId);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(new { count = result.Data });

                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpPatch("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                Guid userId = User.GetUserId();
                var result = await _notificationService.MarkAsReadAsync(notificationId, userId);

                if (result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(result.Message);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpPatch("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                Guid userId = User.GetUserId();
                var result = await _notificationService.MarkAllAsReadAsync(userId);

                if (result.Status == Const.SUCCESS_UPDATE_CODE)
                    return Ok(result.Message);

                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpGet("{notificationId}")]
        public async Task<IActionResult> GetNotificationById(Guid notificationId)
        {
            try
            {
                Guid userId = User.GetUserId();
                var result = await _notificationService.GetNotificationByIdAsync(notificationId, userId);

                if (result.Status == Const.SUCCESS_READ_CODE)
                    return Ok(result.Data);

                if (result.Status == Const.WARNING_NO_DATA_CODE)
                    return NotFound(result.Message);

                return StatusCode(500, result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}



