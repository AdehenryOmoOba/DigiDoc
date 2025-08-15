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
    public class WorkflowController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(
            AppDbContext context,
            INotificationService notificationService,
            ILogger<WorkflowController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost("assign-for-review/{submissionId}")]
        public async Task<IActionResult> AssignForReview(int submissionId, [FromBody] AssignReviewRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.Submitted)
                {
                    return BadRequest("Submission must be in Submitted status to assign for review");
                }

                // Update submission status
                submission.Status = FormStatus.UnderReview;
                submission.IsUnderReview = true;
                submission.AssignedReviewer = request.ReviewerId;
                submission.UpdatedAt = DateTime.UtcNow;

                // Log the assignment
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = User.Identity?.Name ?? "System",
                    Action = "AssignForReview",
                    EntityType = "FormSubmission",
                    EntityId = submissionId,
                    Details = $"Assigned to reviewer: {request.ReviewerId}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Send notification to assigned reviewer
                await _notificationService.SendFormAssignedForReviewNotificationAsync(submission, request.ReviewerId);

                return Ok(new { success = true, message = "Form assigned for review successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning form for review: {SubmissionId}", submissionId);
                return StatusCode(500, "Error assigning form for review");
            }
        }

        [HttpPost("approve/{submissionId}")]
        public async Task<IActionResult> ApproveSubmission(int submissionId, [FromBody] ReviewActionRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.UnderReview)
                {
                    return BadRequest("Submission must be under review to approve");
                }

                // Update submission status
                submission.Status = FormStatus.Approved;
                submission.IsUnderReview = false;
                submission.ReviewedBy = User.Identity?.Name ?? "System";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ApprovedAt = DateTime.UtcNow;
                submission.ReviewNotes = request.Notes;
                submission.UpdatedAt = DateTime.UtcNow;

                // Log the approval
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = User.Identity?.Name ?? "System",
                    Action = "Approve",
                    EntityType = "FormSubmission",
                    EntityId = submissionId,
                    Details = $"Form approved. Notes: {request.Notes}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Send notification to submitter
                await _notificationService.SendFormApprovedNotificationAsync(submission);

                return Ok(new { success = true, message = "Form approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving form: {SubmissionId}", submissionId);
                return StatusCode(500, "Error approving form");
            }
        }

        [HttpPost("return/{submissionId}")]
        public async Task<IActionResult> ReturnSubmission(int submissionId, [FromBody] ReviewActionRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.UnderReview)
                {
                    return BadRequest("Submission must be under review to return");
                }

                // Update submission status
                submission.Status = FormStatus.Returned;
                submission.IsUnderReview = false;
                submission.ReviewedBy = User.Identity?.Name ?? "System";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ReturnedAt = DateTime.UtcNow;
                submission.ReturnReason = request.Notes;
                submission.ReviewAttempts += 1;
                submission.UpdatedAt = DateTime.UtcNow;

                // Log the return
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = User.Identity?.Name ?? "System",
                    Action = "Return",
                    EntityType = "FormSubmission",
                    EntityId = submissionId,
                    Details = $"Form returned. Reason: {request.Notes}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Send notification to submitter
                await _notificationService.SendFormReturnedNotificationAsync(submission, request.Notes);

                return Ok(new { success = true, message = "Form returned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning form: {SubmissionId}", submissionId);
                return StatusCode(500, "Error returning form");
            }
        }

        [HttpPost("reject/{submissionId}")]
        public async Task<IActionResult> RejectSubmission(int submissionId, [FromBody] ReviewActionRequest request)
        {
            try
            {
                var submission = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Include(s => s.Company)
                    .FirstOrDefaultAsync(s => s.Id == submissionId);

                if (submission == null)
                {
                    return NotFound("Submission not found");
                }

                if (submission.Status != FormStatus.UnderReview)
                {
                    return BadRequest("Submission must be under review to reject");
                }

                // Update submission status
                submission.Status = FormStatus.Rejected;
                submission.IsUnderReview = false;
                submission.ReviewedBy = User.Identity?.Name ?? "System";
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ReviewNotes = request.Notes;
                submission.UpdatedAt = DateTime.UtcNow;

                // Log the rejection
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = User.Identity?.Name ?? "System",
                    Action = "Reject",
                    EntityType = "FormSubmission",
                    EntityId = submissionId,
                    Details = $"Form rejected. Reason: {request.Notes}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Send notification to submitter
                await _notificationService.SendFormRejectedNotificationAsync(submission, request.Notes);

                return Ok(new { success = true, message = "Form rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting form: {SubmissionId}", submissionId);
                return StatusCode(500, "Error rejecting form");
            }
        }

        [HttpGet("submissions-by-status/{status}")]
        public async Task<IActionResult> GetSubmissionsByStatus(FormStatus status)
        {
            try
            {
                var submissions = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Include(s => s.Company)
                    .Where(s => s.Status == status)
                    .OrderByDescending(s => s.UpdatedAt)
                    .ToListAsync();

                return Ok(submissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submissions by status: {Status}", status);
                return StatusCode(500, "Error retrieving submissions");
            }
        }

        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetMyAssignments()
        {
            try
            {
                var reviewerId = User.Identity?.Name ?? "";
                var assignments = await _context.FormSubmissions
                    .Include(s => s.FormTemplate)
                    .Include(s => s.Company)
                    .Where(s => s.AssignedReviewer == reviewerId && s.IsUnderReview)
                    .OrderByDescending(s => s.UpdatedAt)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user assignments");
                return StatusCode(500, "Error retrieving assignments");
            }
        }
    }

    public class AssignReviewRequest
    {
        public string ReviewerId { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class ReviewActionRequest
    {
        public string Notes { get; set; } = string.Empty;
    }
} 