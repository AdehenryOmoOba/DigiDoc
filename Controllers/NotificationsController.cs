using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YourApp.Data;
using YourApp.Models;
using YourApp.Services;

namespace YourApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            try
            {
                var userId = User.Identity?.Name ?? "anonymous";
                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                
                var notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && n.Status == NotificationStatus.Unread)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                return Ok(new { count, notifications });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notifications");
                return StatusCode(500, "Error retrieving notifications");
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentNotifications([FromQuery] int limit = 10)
        {
            try
            {
                var userId = User.Identity?.Name ?? "anonymous";
                
                var notifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(limit)
                    .ToListAsync();

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent notifications");
                return StatusCode(500, "Error retrieving notifications");
            }
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkNotificationAsReadAsync(id);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return StatusCode(500, "Error marking notification as read");
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.Identity?.Name ?? "anonymous";
                
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.RecipientId == userId && n.Status == NotificationStatus.Unread)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.Status = NotificationStatus.Read;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, count = unreadNotifications.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "Error marking notifications as read");
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetNotificationCount()
        {
            try
            {
                var userId = User.Identity?.Name ?? "anonymous";
                var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
                
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification count");
                return StatusCode(500, "Error retrieving notification count");
            }
        }
    }
}
