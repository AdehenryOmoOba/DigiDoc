using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigiDocWebApp.Data;
using DigiDocWebApp.Models;
using DigiDocWebApp.Services;

namespace DigiDocWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IFormRenderingService _renderingService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SubmissionsController> _logger;

        public SubmissionsController(
            AppDbContext context,
            IFormRenderingService renderingService,
            INotificationService notificationService,
            ILogger<SubmissionsController> logger)
        {
            _context = context;
            _renderingService = renderingService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetSubmissions([FromQuery] FormStatus? status = null)
        {
            try
            {
                var query = _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .AsQueryable();

                if (status.HasValue)
                {
                    query = query.Where(s => s.Status == status.Value);
                }

                var submissions = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submissions");
                return StatusCode(500, "Error retrieving submissions");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubmission(int id)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                return Ok(submission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submission {SubmissionId}", id);
                return StatusCode(500, "Error retrieving submission");
            }
        }

        [HttpGet("{id}/render")]
        public async Task<IActionResult> RenderSubmission(int id)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                var html = await _renderingService.RenderFormAsync(submission.FormTemplate, submission);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering submission {SubmissionId}", id);
                return StatusCode(500, "Error rendering submission");
            }
        }

        [HttpPost("{id}/review")]
        public async Task<IActionResult> StartReview(int id)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.Submitted)
                {
                    return BadRequest("Submission is not in submitted status");
                }

                submission.Status = FormStatus.UnderReview;
                submission.ReviewedBy = User.Identity?.Name ?? "system";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Started review for submission {SubmissionId}", id);

                return Ok(new { success = true, message = "Review started successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting review for submission {SubmissionId}", id);
                return StatusCode(500, "Error starting review");
            }
        }

        [HttpPost("{id}/return")]
        public async Task<IActionResult> ReturnSubmission(int id, [FromBody] ReturnSubmissionRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.UnderReview && submission.Status != FormStatus.Submitted)
                {
                    return BadRequest("Submission cannot be returned in current status");
                }

                submission.Status = FormStatus.Returned;
                submission.ReviewedBy = User.Identity?.Name ?? "system";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ReturnedAt = DateTime.UtcNow;
                submission.ReturnReason = request.Reason;
                submission.ReviewNotes = request.Notes;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification
                await _notificationService.SendFormReturnedNotificationAsync(submission, request.Reason);

                _logger.LogInformation("Returned submission {SubmissionId} with reason: {Reason}", id, request.Reason);

                return Ok(new { success = true, message = "Submission returned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning submission {SubmissionId}", id);
                return StatusCode(500, "Error returning submission");
            }
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveSubmission(int id, [FromBody] ApproveSubmissionRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.UnderReview)
                {
                    return BadRequest("Submission is not under review");
                }

                submission.Status = FormStatus.Approved;
                submission.ReviewedBy = User.Identity?.Name ?? "system";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ApprovedAt = DateTime.UtcNow;
                submission.ReviewNotes = request.Notes;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification
                await _notificationService.SendFormApprovedNotificationAsync(submission);

                _logger.LogInformation("Approved submission {SubmissionId}", id);

                return Ok(new { success = true, message = "Submission approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving submission {SubmissionId}", id);
                return StatusCode(500, "Error approving submission");
            }
        }

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectSubmission(int id, [FromBody] RejectSubmissionRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.UnderReview)
                {
                    return BadRequest("Submission is not under review");
                }

                submission.Status = FormStatus.Rejected;
                submission.ReviewedBy = User.Identity?.Name ?? "system";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ReviewNotes = request.Notes;
                submission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification
                await _notificationService.SendFormRejectedNotificationAsync(submission, request.Reason);

                _logger.LogInformation("Rejected submission {SubmissionId} with reason: {Reason}", id, request.Reason);

                return Ok(new { success = true, message = "Submission rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting submission {SubmissionId}", id);
                return StatusCode(500, "Error rejecting submission");
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    TotalSubmissions = await _context.FormSubmissions.CountAsync(),
                    PendingReview = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Submitted),
                    UnderReview = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.UnderReview),
                    Approved = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Approved),
                    Returned = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Returned),
                    Rejected = await _context.FormSubmissions.CountAsync(s => s.Status == FormStatus.Rejected)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard stats");
                return StatusCode(500, "Error retrieving dashboard stats");
            }
        }
    }

    public class ReturnSubmissionRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ApproveSubmissionRequest
    {
        public string? Notes { get; set; }
    }

    public class RejectSubmissionRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
