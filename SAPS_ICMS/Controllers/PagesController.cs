using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPS_ICMS.Data;
using SAPS_ICMS.Filters;
using SAPS_ICMS.Models;
using SAPS_ICMS.Services;
using System.Security.Claims;
using System.Text;

namespace SAPS_ICMS.Controllers
{
    public class PagesController : Controller
    {
        private readonly UserManager<Officer> _userManager;
        private readonly SignInManager<Officer> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogService;
        public PagesController(UserManager<Officer> userManager, SignInManager<Officer> signInManager, ApplicationDbContext context, IActivityLogService activityLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _activityLogService = activityLogService;
        }
        public async Task<IActionResult> Dashboard()
        {
            var cases = await _context.Cases.ToListAsync();

            // Get pending closure requests count for admin
            if (User.IsInRole("Admin"))
            {
                ViewBag.PendingRequests = await _context.CaseClosureRequests
                    .CountAsync(r => r.Status == RequestStatus.Pending);

                ViewBag.PendingEditRequests = await _context.CaseEditRequests
                    .CountAsync(r => r.Status == EditRequestStatus.Pending);
            }
            else if (User.IsInRole("Detective"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.ApprovedEditRequests = await _context.CaseEditRequests
                    .CountAsync(r => r.RequestedByUserId == userId &&
                                    r.Status == EditRequestStatus.Approved &&
                                    !r.EditCompleted);
            }

            return View(cases);
        }

        public IActionResult Tasks()
        {
            return View();
        }

        public IActionResult Calendar()
        {
            return View();
        }
        public IActionResult Help()
        {
            return View();
        }
        public async Task<IActionResult> Archive(string searchString)
        {
            // Check if password has been verified in this session
            var isVerified = HttpContext.Session.GetString("ArchiveAccessVerified");

            if (isVerified != "true")
            {
                // Redirect to password verification
                return RedirectToAction("VerifyPasswordForArchive", new { returnUrl = Url.Action("Archive") });
            }

            // Check if verification is still valid (optional - expire after 30 minutes)
            var verificationTime = HttpContext.Session.GetString("ArchiveAccessTime");
            if (!string.IsNullOrEmpty(verificationTime))
            {
                if (DateTime.TryParse(verificationTime, out DateTime accessTime))
                {
                    if (DateTime.Now.Subtract(accessTime).TotalMinutes > 2)
                    {
                        // Verification expired, require re-verification
                        HttpContext.Session.Remove("ArchiveAccessVerified");
                        HttpContext.Session.Remove("ArchiveAccessTime");
                        TempData["ErrorMessage"] = "Your archive access has expired. Please try again.";
                        return RedirectToAction("VerifyPasswordForArchive", new { returnUrl = Url.Action("Archive") });
                    }
                }
            }

            // Password verified, proceed with archive access
            IQueryable<Case> closedCasesQuery = _context.Cases
                .Where(c => c.Status == CaseStatus.Closed);

            // Filter by assigned locations for admins
            //if (User.IsInRole("Admin"))
            //{
            //    var assignedLocations = await GetAssignedLocationsAsync();
            //    if (assignedLocations.Any())
            //    {
            //        closedCasesQuery = closedCasesQuery.Where(c => assignedLocations.Contains(c.LocationTaken));
            //    }
            //}

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                closedCasesQuery = closedCasesQuery.Where(c =>
                    c.CaseId.ToString().Contains(searchString) ||
                    (c.Complainant != null && c.Complainant.ToLower().Contains(lowerSearch)) ||
                    (c.LocationTaken != null && c.LocationTaken.ToLower().Contains(lowerSearch)) ||
                    (c.CaseInformation != null && c.CaseInformation.ToLower().Contains(lowerSearch)) ||
                    (c.OfficerTakingStatement != null && c.OfficerTakingStatement.ToLower().Contains(lowerSearch))
                );
            }

            var closedCases = await closedCasesQuery
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            ViewBag.SearchString = searchString;

            return View(closedCases);
        }

        [Authorize]
        [HttpPost]
        public IActionResult RefreshArchiveAccess()
        {
            var isVerified = HttpContext.Session.GetString("ArchiveAccessVerified");

            if (isVerified == "true")
            {
                // Update the access time
                HttpContext.Session.SetString("ArchiveAccessTime", DateTime.Now.ToString());
                return Json(new { success = true, message = "Access time refreshed" });
            }

            return Json(new { success = false, message = "Not verified" });
        }

        [Authorize]
        [HttpPost]
        public IActionResult ClearArchiveAccess()
        {
            HttpContext.Session.Remove("ArchiveAccessVerified");
            HttpContext.Session.Remove("ArchiveAccessTime");
            TempData["SuccessMessage"] = "Archive access cleared. You'll need to verify your password again.";
            return RedirectToAction("Dashboard");
        }

        [CustomAuthorize(Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> CloseCase([FromBody] CaseActionRequest request)
        {
            if (request == null || request.Id <= 0)
                return BadRequest(new { success = false, message = "Invalid case ID" });

            var caseItem = await _context.Cases
                .FirstOrDefaultAsync(c => c.CaseId == request.Id);

            if (caseItem == null)
                return NotFound(new { success = false, message = "Case not found" });

            if (caseItem.Status == CaseStatus.Closed)
                return BadRequest(new { success = false, message = "Case is already closed" });

            caseItem.Status = CaseStatus.Closed;
            

            // Mark any pending closure requests as approved
            var pendingRequests = await _context.CaseClosureRequests
                .Where(r => r.CaseId == request.Id && r.Status == RequestStatus.Pending)
                .ToListAsync();

            foreach (var req in pendingRequests)
            {
                req.Status = RequestStatus.Approved;
                req.ReviewedDate = DateTime.Now;
                req.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                req.ReviewedByUserName = User.Identity.Name;
            }

            try
            {
                await _context.SaveChangesAsync();

                await _activityLogService.LogActivityAsync(
    ActivityActions.CaseClosed,
    "Case",
    request.Id,
    $"Case closed by admin"
);

                return Json(new { success = true, message = "Case closed successfully" });
            }
            catch (DbUpdateException ex)
            {
                
                return StatusCode(500, new { success = false, message = "Error closing case" });
            }
        }

        // OFFICER - Request case closure
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCaseClosure([FromForm] CaseClosureRequestDto request)
        {
            if (request == null || request.CaseId <= 0)
                return BadRequest(new { success = false, message = "Invalid case ID" });

            var caseItem = await _context.Cases
                .FirstOrDefaultAsync(c => c.CaseId == request.CaseId);

            if (caseItem == null)
                return NotFound(new { success = false, message = "Case not found" });

            if (caseItem.Status == CaseStatus.Closed)
                return BadRequest(new { success = false, message = "Case is already closed" });

            // Check if there's already a pending request for this case
            var existingRequest = await _context.CaseClosureRequests
                .AnyAsync(r => r.CaseId == request.CaseId && r.Status == RequestStatus.Pending);

            if (existingRequest)
                return BadRequest(new { success = false, message = "A closure request is already pending for this case" });

            var closureRequest = new CaseClosureRequest
            {
                CaseId = request.CaseId,
                RequestedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                RequestedByUserName = User.Identity.Name,
                RequestedDate = DateTime.Now,
                Reason = request.Reason,
                Status = RequestStatus.Pending
            };

            Console.WriteLine($"Creating closure request:");
            Console.WriteLine($"CaseId: {closureRequest.CaseId}");
            Console.WriteLine($"RequestedByUserId: {closureRequest.RequestedByUserId}");
            Console.WriteLine($"RequestedByUserName: {closureRequest.RequestedByUserName}");
            Console.WriteLine($"ReviewedByUserId: {closureRequest.ReviewedByUserId}"); 
            Console.WriteLine($"Status: {closureRequest.Status}");

            try
            {
                _context.CaseClosureRequests.Add(closureRequest);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Case closure request sent to admin for review" });
            }
                catch (DbUpdateException ex)
            {
                
                Console.WriteLine($"Database error: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { success = false, message = $"Error submitting request: {ex.InnerException?.Message}" });
            }
        }

        // ADMIN - View pending closure requests
        [CustomAuthorize(Roles.Admin)]
        public async Task<IActionResult> PendingClosureRequests()
        {
            var requests = await _context.CaseClosureRequests
                .Include(r => r.Case)
                .Where(r => r.Status == RequestStatus.Pending)
                .OrderByDescending(r => r.RequestedDate)
                .ToListAsync();

            return View(requests);
        }

        // ADMIN - Approve or Reject closure request
        [CustomAuthorize(Roles.Admin)]
        [HttpPost]
       
        public async Task<IActionResult> ReviewClosureRequest([FromBody] ReviewRequestDto review)
        {
            if (review == null || review.RequestId <= 0)
                return BadRequest(new { success = false, message = "Invalid request" });

            var closureRequest = await _context.CaseClosureRequests
                .Include(r => r.Case)
                .FirstOrDefaultAsync(r => r.RequestId == review.RequestId);

            if (closureRequest == null)
                return NotFound(new { success = false, message = "Request not found" });

            if (closureRequest.Status != RequestStatus.Pending)
                return BadRequest(new { success = false, message = "Request has already been reviewed" });

            closureRequest.Status = review.Approve ? RequestStatus.Approved : RequestStatus.Rejected;
            closureRequest.ReviewedDate = DateTime.Now;
            closureRequest.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            closureRequest.ReviewedByUserName = User.Identity.Name;
            

            // If approved, close the case
            if (review.Approve)
            {
                closureRequest.Case.Status = CaseStatus.Closed;
              
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = review.Approve ? "Case closed successfully" : "Request rejected"
                });
            }
            catch (DbUpdateException ex)
            {
                // Log the exception
                return StatusCode(500, new { success = false, message = "Error processing request" });
            }
        }
        

        [CustomAuthorize(Roles.Admin)]
        public async Task<IActionResult> CasesByLocation(string location)
        {
            // Get all unique locations
            var locations = await _context.Cases
                .Where(c => c.LocationTaken != null)
                .Select(c => c.LocationTaken)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            ViewBag.Locations = locations;
            ViewBag.SelectedLocation = location;

            // If a location is selected, get cases for that location
            List<Case> cases = new List<Case>();
            if (!string.IsNullOrEmpty(location))
            {
                cases = await _context.Cases
                    .Where(c => c.LocationTaken == location)
                    .OrderByDescending(c => c.Date)
                    .ToListAsync();
            }
            else
            {
                // If no location selected, get all cases grouped by location
                cases = await _context.Cases
                    .Where(c => c.LocationTaken != null)
                    .OrderBy(c => c.LocationTaken)
                    .ThenByDescending(c => c.Date)
                    .ToListAsync();
            }

            return View(cases);
        }
        // DETECTIVE - Request case edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles.Detective)]
        public async Task<IActionResult> RequestCaseEdit([FromForm] CaseEditRequestDto request)
        {
            if (request == null || request.CaseId <= 0)
                return BadRequest(new { success = false, message = "Invalid case ID" });

            var caseItem = await _context.Cases
                .FirstOrDefaultAsync(c => c.CaseId == request.CaseId);

            if (caseItem == null)
                return NotFound(new { success = false, message = "Case not found" });

            if (caseItem.Status == CaseStatus.Closed)
                return BadRequest(new { success = false, message = "Cannot request edit for closed cases" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
            // Check if there's already a pending request for this case
            var existingRequest = await _context.CaseEditRequests
                .AnyAsync(r => r.CaseId == request.CaseId &&
                      r.RequestedByUserId == userId &&
                      ((r.Status == EditRequestStatus.Pending) ||
                       (r.Status == EditRequestStatus.Approved && !r.EditCompleted)));

            if (existingRequest)
                return BadRequest(new { success = false, message = "An edit request is already pending for this case" });

            var editRequest = new CaseEditRequest
            {
                CaseId = request.CaseId,
                RequestedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown",
                RequestedByUserName = User.Identity?.Name ?? "Unknown User",
                RequestedDate = DateTime.Now,
                RequestedChanges = request.RequestedChanges,
                Justification = request.Justification,
                Status = EditRequestStatus.Pending
            };

            try
            {
                _context.CaseEditRequests.Add(editRequest);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Edit request sent to admin for review" });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error submitting request" });
            }
        }

        
        [CustomAuthorize(Roles.Admin)]
        public async Task<IActionResult> PendingEditRequests()
        {
            var requests = await _context.CaseEditRequests
                .Include(r => r.Case)
                .Where(r => r.Status == EditRequestStatus.Pending)
                .OrderByDescending(r => r.RequestedDate)
                .ToListAsync();

            return View(requests);
        }

       
        [CustomAuthorize(Roles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewEditRequest( ReviewEditRequestDto review)
        {
            if (review == null || review.RequestId <= 0)
                return BadRequest(new { success = false, message = "Invalid request" });

            var editRequest = await _context.CaseEditRequests
                .Include(r => r.Case)
                .FirstOrDefaultAsync(r => r.EditRequestId == review.RequestId);

            if (editRequest == null)
                return NotFound(new { success = false, message = "Request not found" });

            if (editRequest.Status != EditRequestStatus.Pending)
                return BadRequest(new { success = false, message = "Request has already been reviewed" });

            editRequest.Status = review.Approve ? EditRequestStatus.Approved : EditRequestStatus.Rejected;
            editRequest.ReviewedDate = DateTime.Now;
            editRequest.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            editRequest.ReviewedByUserName = User.Identity?.Name;
            editRequest.AdminNotes = review.AdminNotes;

            try
            {
                await _context.SaveChangesAsync();

                // If approved, redirect admin to edit page
                if (review.Approve)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Request approved. Detective can now edit the case"
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Request rejected"
                });
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error processing request" });
            }
        }

        [CustomAuthorize(Roles.Detective)]
        public async Task<IActionResult> MyApprovedEditRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;

            Console.WriteLine($"=== MyApprovedEditRequests Debug ===");
            Console.WriteLine($"Current User ID: {userId}");
            Console.WriteLine($"Current User Name: {userName}");

            var allRequests = await _context.CaseEditRequests.ToListAsync();
            Console.WriteLine($"Total edit requests in DB: {allRequests.Count}");

            foreach (var req in allRequests)
            {
                Console.WriteLine($"Request {req.EditRequestId}: CaseId={req.CaseId}, RequestedBy={req.RequestedByUserId}, Status={req.Status}, Completed={req.EditCompleted}");
            }

            var approvedRequests = await _context.CaseEditRequests
                .Include(r => r.Case)
                .Where(r => r.RequestedByUserId == userId &&
                            r.Status == EditRequestStatus.Approved &&
                            !r.EditCompleted)
                .OrderByDescending(r => r.ReviewedDate)
                .ToListAsync();

            Console.WriteLine($"Filtered approved requests for this user: {approvedRequests.Count}");
            Console.WriteLine($"=== End Debug ===");

            return View(approvedRequests);
        }

        [CustomAuthorize(Roles.Detective)]
        [HttpPost]
        public async Task<IActionResult> CompleteEdit(int editRequestId)
        {
            var editRequest = await _context.CaseEditRequests
                .FirstOrDefaultAsync(r => r.EditRequestId == editRequestId);

            if (editRequest == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (editRequest.RequestedByUserId != userId)
                return Forbid();

            editRequest.EditCompleted = true;
            editRequest.EditCompletedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Edit completed successfully";
            return RedirectToAction("MyApprovedEditRequests");
        }

        // ADMIN or DETECTIVE - Clean up duplicate edit requests
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CleanupDuplicateEditRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all approved, uncompleted requests for the user
            var allRequests = await _context.CaseEditRequests
                .Where(r => r.RequestedByUserId == userId &&
                            r.Status == EditRequestStatus.Approved &&
                            !r.EditCompleted)
                .OrderByDescending(r => r.ReviewedDate)
                .ToListAsync();

            // Group by CaseId
            var grouped = allRequests.GroupBy(r => r.CaseId);

            int cleanedCount = 0;

            foreach (var group in grouped)
            {
                // Keep the first (most recent) request
                var toKeep = group.First();

                // Mark all others as completed
                foreach (var duplicate in group.Skip(1))
                {
                    duplicate.EditCompleted = true;
                    duplicate.EditCompletedDate = DateTime.Now;
                    cleanedCount++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cleaned up {cleanedCount} duplicate edit requests";
            return RedirectToAction("MyApprovedEditRequests");
        }
        // DETECTIVE - Cancel/Remove specific edit request
        [CustomAuthorize(Roles.Detective)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEditRequest(int editRequestId)
        {
            Console.WriteLine($"=== CancelEditRequest called ===");
            Console.WriteLine($"EditRequestId: {editRequestId}");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"UserId: {userId}");

            var editRequest = await _context.CaseEditRequests
                .FirstOrDefaultAsync(r => r.EditRequestId == editRequestId &&
                                          r.RequestedByUserId == userId);

            if (editRequest == null)
            {
                Console.WriteLine("Edit request not found or not authorized");
                return NotFound(new { success = false, message = "Request not found or you don't have permission" });
            }

            Console.WriteLine($"Found request: CaseId={editRequest.CaseId}, Status={editRequest.Status}");

            // Mark as completed to remove from approved list
            editRequest.EditCompleted = true;
            editRequest.EditCompletedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            Console.WriteLine("Request marked as completed");

            return Json(new { success = true, message = "Edit request removed successfully" });
        }
        [Authorize]
        [HttpGet]
        public IActionResult VerifyPasswordForArchive(string returnUrl = null)
        {
            var model = new PasswordVerificationModel
            {
                ReturnUrl = returnUrl ?? Url.Action("Archive")
            };

            return View(model);
        }

        // POST: Verify password
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPasswordForArchive(PasswordVerificationModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Verify the password
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Set a session flag or cookie to remember verification
                HttpContext.Session.SetString("ArchiveAccessVerified", "true");
                HttpContext.Session.SetString("ArchiveAccessTime", DateTime.Now.ToString());

                // Redirect to archive
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Archive");
            }
            else
            {
                TempData["ErrorMessage"] = "Incorrect password. Please try again.";
                return View(model);
            }
        }

        // GET: Activity Log for Admin
        [CustomAuthorize(Roles.Admin)]
        public async Task<IActionResult> ActivityLog(string searchUser, string searchAction, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = 50;

            var logsQuery = _context.ActivityLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchUser))
            {
                logsQuery = logsQuery.Where(l => l.UserName.Contains(searchUser) || l.UserId.Contains(searchUser));
            }

            if (!string.IsNullOrEmpty(searchAction))
            {
                logsQuery = logsQuery.Where(l => l.Action.Contains(searchAction));
            }

            if (fromDate.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                // Include the entire day
                var endDate = toDate.Value.AddDays(1);
                logsQuery = logsQuery.Where(l => l.Timestamp < endDate);
            }

            // Get total count for pagination
            var totalLogs = await logsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

            // Get paginated results
            var logs = await logsQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get statistics
            var stats = new
            {
                TotalLogs = totalLogs,
                TodayLogs = await _context.ActivityLogs.CountAsync(l => l.Timestamp >= DateTime.Today),
                UniqueUsers = await _context.ActivityLogs.Select(l => l.UserId).Distinct().CountAsync(),
                MostCommonAction = await _context.ActivityLogs
                    .GroupBy(l => l.Action)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .FirstOrDefaultAsync()
            };

            ViewBag.Statistics = stats;
            ViewBag.SearchUser = searchUser;
            ViewBag.SearchAction = searchAction;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(logs);
        }

        // Export activity logs to CSV
        [CustomAuthorize(Roles.Admin)]
        public async Task<IActionResult> ExportActivityLog(string searchUser, string searchAction, DateTime? fromDate, DateTime? toDate)
        {
            var logsQuery = _context.ActivityLogs.AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
                logsQuery = logsQuery.Where(l => l.UserName.Contains(searchUser) || l.UserId.Contains(searchUser));

            if (!string.IsNullOrEmpty(searchAction))
                logsQuery = logsQuery.Where(l => l.Action.Contains(searchAction));

            if (fromDate.HasValue)
                logsQuery = logsQuery.Where(l => l.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                logsQuery = logsQuery.Where(l => l.Timestamp < toDate.Value.AddDays(1));

            var logs = await logsQuery.OrderByDescending(l => l.Timestamp).ToListAsync();

            // Generate CSV
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,User,Action,Entity Type,Entity ID,Details,IP Address");

            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.UserName}\",\"{log.Action}\",\"{log.EntityType}\",\"{log.EntityId}\",\"{log.Details}\",\"{log.IpAddress}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"ActivityLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }

    public class CaseEditRequestDto
    {
        public int CaseId { get; set; }
        public string RequestedChanges { get; set; }
        public string? Justification { get; set; }
    }

    public class ReviewEditRequestDto
    {
        public int RequestId { get; set; }
        public bool Approve { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class CaseActionRequest
    {
        public int Id { get; set; }
    }

    public class CaseClosureRequestDto
    {
        public int CaseId { get; set; }
        public string Reason { get; set; }
    }

    public class ReviewRequestDto
    {
        public int RequestId { get; set; }
        public bool Approve { get; set; }
        
    }
}
