using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPS_ICMS.Data;
using SAPS_ICMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SAPS_ICMS.Controllers
{
    [Authorize]
    public class EvidenceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Officer> _userManager;

        public EvidenceController(ApplicationDbContext context, UserManager<Officer> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var officer = await _userManager.GetUserAsync(User);
            var evidenceQuery = _context.Evidences
                .Include(e => e.Case)
                .AsQueryable();

            if(!User.IsInRole("Admin") && !User.IsInRole("Detective"))
            {
                var officerName = $"{officer.Name}{officer.Surname}";
                evidenceQuery = evidenceQuery.Where(e => e.Case.OfficerTakingStatement == officer.Name);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                evidenceQuery = evidenceQuery.Where(e =>
                    (e.FileName != null && e.FileName.ToLower().Contains(lowerSearch)) ||
                    e.CaseId.ToString().Contains(searchString) ||
                    (e.UploadedBy != null && e.UploadedBy.ToLower().Contains(lowerSearch)));
            }

            var evidenceList = await evidenceQuery
                .OrderByDescending(e => e.UploadDate)
                .ToListAsync();

            ViewBag.SearchString = searchString;

            return View(evidenceList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, int caseId)
        {
            if (file != null && file.Length > 0)
            {
                var officer = await _userManager.GetUserAsync(User);

                var evidence = new Evidence
                {
                    FileName = file.FileName,
                    CaseId = caseId,
                    UploadedBy = $"{officer.Name}{officer.Surname}",   
                    UploadDate = DateTime.Now
                };

                _context.Evidences.Add(evidence);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Evidence uploaded successfully!";
            }

            return RedirectToAction("Index");
        }

        // Download a file
        public async Task<IActionResult> Download(int id)
        {
            var evidence = await _context.Evidences.FindAsync(id);
            if (evidence == null) return NotFound();

            return File(evidence.FileData, evidence.ContentType, evidence.FileName);
        }
    }
}
