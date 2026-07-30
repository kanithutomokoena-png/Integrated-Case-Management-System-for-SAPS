using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPS_ICMS.Data;
using SAPS_ICMS.Filters;
using SAPS_ICMS.Models;
using SAPS_ICMS.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SAPS_ICMS.Controllers
{
    [Authorize]
    public class CasesController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<Officer> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IActivityLogService _activityLogService;
        public CasesController(ApplicationDbContext context, UserManager<Officer> manager, IWebHostEnvironment env, IActivityLogService activityLogService)
        {
            _context = context;
            _userManager = manager;
            _env = env;
            _activityLogService = activityLogService; 
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tempCaseNumber = $"TEMP-{DateTime.Now:yyyyMMddHHmmss}";
            ViewBag.TempCaseNumber = tempCaseNumber;
            ViewBag.Officer = User.Identity?.Name;

            var officer = await _userManager.GetUserAsync(User);
            ViewBag.Officer = officer?.Name + " " + officer?.Surname;

            return View(new Case());
        }

        public async Task<IActionResult> Index()
        {
            
            var casesQuery = _context.Cases.AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var officer = await _userManager.GetUserAsync(User);
                casesQuery = casesQuery.Where(c => c.OfficerTakingStatement == officer.Name + " " + officer.Surname);
            }

            var cases = await casesQuery.ToListAsync();
            return View(cases);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Case model, List<IFormFile> EvidenceFiles)
        {
            // Remove validation for complex objects and auto-generated fields
            ModelState.Remove("Witnesses");
            ModelState.Remove("Evidences");
            ModelState.Remove("EvidenceFiles");
            ModelState.Remove("CaseNumber");

            try
            {
                var officer = await _userManager.GetUserAsync(User);
                model.OfficerTakingStatement = officer?.Name + " " + officer?.Surname;

                // Generate case number based on location
                model.CaseNumber = await GenerateCaseNumberAsync();

                System.Diagnostics.Debug.WriteLine("=== Starting Case Creation ===");
                System.Diagnostics.Debug.WriteLine($"Case Number: {model.CaseNumber}");
                System.Diagnostics.Debug.WriteLine($"Officer: {model.OfficerTakingStatement}");
                System.Diagnostics.Debug.WriteLine($"Location: {model.LocationTaken}");

                // Set default status if not set
                if (model.Status == 0)
                {
                    model.Status = CaseStatus.Investigation;
                }

                // Save the case first to get CaseId
                _context.Cases.Add(model);
                await _context.SaveChangesAsync(); 

                System.Diagnostics.Debug.WriteLine($"Case saved! CaseId: {model.CaseId}");

                // Handle Witnesses
                var witnessNames = Request.Form["Witnesses[0].Name"].ToList();
                var witnessPhones = Request.Form["Witnesses[0].PhoneNumber"].ToList();
                var witnessStatements = Request.Form["Witnesses[0].Statement"].ToList();

                int witnessCount = 0;
                if (witnessNames.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Processing {witnessNames.Count} potential witnesses...");

                    for (int i = 0; i < witnessNames.Count; i++)
                    {
                        
                        if (string.IsNullOrWhiteSpace(witnessNames[i]))
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping empty witness at index {i}");
                            continue;
                        }

                        var witness = new Witness
                        {
                            CaseId = model.CaseId,
                            Name = witnessNames[i],
                            PhoneNumber = i < witnessPhones.Count ? witnessPhones[i] : null,
                            Statement = i < witnessStatements.Count ? witnessStatements[i] : null
                        };

                        _context.Witnesses.Add(witness);
                        System.Diagnostics.Debug.WriteLine($"Added witness {witnessCount + 1}: {witness.Name}");
                        witnessCount++;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"Total witnesses added: {witnessCount}");

                // Handle Evidence Files
                int evidenceCount = 0;
                if (EvidenceFiles != null && EvidenceFiles.Any(f => f != null && f.Length > 0))
                {
                    System.Diagnostics.Debug.WriteLine($"Processing {EvidenceFiles.Count} evidence files...");

                    foreach (var file in EvidenceFiles)
                    {
                        if (file == null || file.Length == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Skipping empty file");
                            continue;
                        }

                        System.Diagnostics.Debug.WriteLine($"Processing file: Name='{file.FileName}', Length={file.Length}");

                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);

                            var evidence = new Evidence
                            {
                                CaseId = model.CaseId,
                                FileName = file.FileName,
                                ContentType = file.ContentType,
                                FileData = memoryStream.ToArray(),
                                UploadDate = DateTime.Now,
                                UploadedBy = officer?.Name + " " + officer?.Surname
                            };

                            _context.Evidences.Add(evidence);
                            System.Diagnostics.Debug.WriteLine($"Added evidence: {file.FileName}");
                            evidenceCount++;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No evidence files provided");
                }
                System.Diagnostics.Debug.WriteLine($"Total evidence files added: {evidenceCount}");

                
                if (witnessCount > 0 || evidenceCount > 0)
                {
                    await _context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine("Witnesses and evidence saved");
                }

                System.Diagnostics.Debug.WriteLine("=== Case Creation Complete ===");

                //LOG ACTIVITY
                await _activityLogService.LogActivityAsync(
                    ActivityActions.CaseCreated,
                    "Case",
                    model.CaseId,
                    $"Created case {model.CaseNumber} at {model.LocationTaken}"
                );

                TempData["SuccessMessage"] = $"Case {model.CaseNumber} created successfully!";
                return RedirectToAction("Details", new { id = model.CaseId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");

                // Log ModelState errors
                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }

                TempData["ErrorMessage"] = $"Error creating case: {ex.Message}";

                var officer2 = await _userManager.GetUserAsync(User);
                ViewBag.Officer = officer2?.Name + " " + officer2?.Surname;

                return View(model);
            }
        }
        private async Task<string> GenerateCaseNumberAsync()
        {
            

            var year = DateTime.Now.Year;
            var location = Request.Form["LocationTaken"].ToString();

           
            var locationCode = GetLocationCode(location);

            
            var lastCase = await _context.Cases
                .Where(c => c.CaseNumber != null &&
                            c.CaseNumber.StartsWith($"{locationCode}-{year}"))
                .OrderByDescending(c => c.CaseNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;

            if (lastCase != null && !string.IsNullOrEmpty(lastCase.CaseNumber))
            {
                // Extract sequence number from last case number
                var parts = lastCase.CaseNumber.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }

            
            return $"{locationCode}-{year}-{sequence:D4}";
        }

        private string GetLocationCode(string location)
        {
            // Map location names to codes
            return location switch
            {
                "ParkRoad" => "PR",
                "Bayswater" => "BW",
                "Navalsig" => "NV",
                "Bainsvlei" => "BV",
                "Glen" => "GL",
                _ => "XX"
            };
        }

        public async Task<IActionResult> Details(int id)
        {
            var caseItem = _context.Cases
                .Include(c => c.Witnesses)
                .FirstOrDefault(c => c.CaseId == id);

            if (caseItem == null)
            {
                return NotFound();
            }

            if (!User.IsInRole(Roles.Admin))
            {
                var officer = await _userManager.GetUserAsync(User);
                if (caseItem.OfficerTakingStatement != officer?.Name + " " + officer?.Surname)
                {
                    TempData["Error"] = "You don't have permission to view this case";
                    return RedirectToAction("Index");
                }
            }

            var evidence = _context.Evidences
                    .Where(e => e.CaseId == id)
                    .OrderByDescending(e => e.UploadDate)
                    .ToList();

            ViewBag.EvidenceList = evidence;
            return View(caseItem);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var caseItem = await _context.Cases
                .Include(c => c.Witnesses)
                .Include(c => c.Evidences)
                .FirstOrDefaultAsync(c => c.CaseId == id);

            if (caseItem == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Admin"))
            {
                return View(caseItem);
            }
            else if (User.IsInRole("Detective"))
            {
                // Detective can only edit if they have an approved, uncompleted request
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var hasApprovedRequest = await _context.CaseEditRequests
                    .AnyAsync(r => r.CaseId == id &&
                                  r.RequestedByUserId == userId &&
                                  r.Status == EditRequestStatus.Approved &&
                                  !r.EditCompleted);

                if (!hasApprovedRequest)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this case. Request edit access first.";
                    return RedirectToAction("Details", new { id });
                }

                ViewBag.EditRequestMode = true; 
                return View(caseItem);
            }
            else
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Case model, List<IFormFile> EvidenceFiles)
        {
            // Remove Witnesses from ModelState validation
            ModelState.Remove("Witnesses");

            Console.WriteLine("=== Edit Form Submission Debug ===");
            Console.WriteLine($"Case ID: {model.CaseId}");

            
            foreach (var key in Request.Form.Keys)
            {
                Console.WriteLine($"Form Key: {key} = {Request.Form[key]}");
            }

            if (ModelState.IsValid || true) 
            {
                try
                {
                    var existingCase = await _context.Cases
                        .Include(c => c.Witnesses)
                        .Include(c => c.Evidences)
                        .FirstOrDefaultAsync(c => c.CaseId == model.CaseId);

                    if (existingCase == null)
                    {
                        return NotFound();
                    }

                    // Check permissions
                    if (!User.IsInRole("Admin") && User.IsInRole("Detective"))
                    {
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        var editRequest = await _context.CaseEditRequests
                            .FirstOrDefaultAsync(r => r.CaseId == model.CaseId &&
                                                    r.RequestedByUserId == userId &&
                                                    r.Status == EditRequestStatus.Approved &&
                                                    !r.EditCompleted);

                        if (editRequest == null)
                        {
                            return Forbid();
                        }

                        editRequest.EditCompleted = true;
                        editRequest.EditCompletedDate = DateTime.Now;
                    }

                    // Update case properties
                    existingCase.Date = model.Date;
                    existingCase.LocationTaken = model.LocationTaken;
                    existingCase.OfficerTakingStatement = model.OfficerTakingStatement;
                    existingCase.Complainant = model.Complainant;
                    existingCase.DateOfBirth = model.DateOfBirth;
                    existingCase.HomeAddress = model.HomeAddress;
                    existingCase.PhoneNumber = model.PhoneNumber;
                    existingCase.Occupation = model.Occupation;
                    existingCase.RelationshipToIncident = model.RelationshipToIncident;
                    existingCase.CaseInformation = model.CaseInformation;
                    existingCase.ComplainantSignature = model.ComplainantSignature;
                    existingCase.ComplainantSignatureDate = model.ComplainantSignatureDate;
                    existingCase.ComplainantSignatureTime = model.ComplainantSignatureTime;
                    existingCase.OfficerSignature = model.OfficerSignature;
                    existingCase.OfficerSignatureDate = model.OfficerSignatureDate;

                    if (User.IsInRole("Admin"))
                    {
                        existingCase.Status = model.Status;
                    }

                    // Handle new evidence files
                    if (EvidenceFiles != null && EvidenceFiles.Any())
                    {
                        var officer = await _userManager.GetUserAsync(User);
                        foreach (var file in EvidenceFiles)
                        {
                            if (file != null && file.Length > 0)
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await file.CopyToAsync(memoryStream);
                                    var evidence = new Evidence
                                    {
                                        FileName = file.FileName,
                                        ContentType = file.ContentType,
                                        FileData = memoryStream.ToArray(),
                                        CaseId = model.CaseId,
                                        UploadedBy = $"{officer.Name} {officer.Surname}",
                                        UploadDate = DateTime.Now
                                    };
                                    _context.Evidences.Add(evidence);
                                }
                            }
                        }
                    }

                    
                    Console.WriteLine("=== Processing Witnesses ===");

                    // Get all witness-related form fields
                    var allWitnessKeys = Request.Form.Keys.Where(k => k.StartsWith("Witnesses[")).ToList();
                    Console.WriteLine($"Found {allWitnessKeys.Count} witness form fields");

                   
                    var witnessData = new Dictionary<int, Dictionary<string, string>>();

                    foreach (var key in allWitnessKeys)
                    {
                        
                        var match = System.Text.RegularExpressions.Regex.Match(key, @"Witnesses\[(\d*)\]\.(\w+)");
                        if (match.Success)
                        {
                            var indexStr = match.Groups[1].Value;
                            var propertyName = match.Groups[2].Value;
                            var value = Request.Form[key].ToString();

                            int index = int.Parse(indexStr);

                            if (!witnessData.ContainsKey(index))
                            {
                                witnessData[index] = new Dictionary<string, string>();
                            }

                            witnessData[index][propertyName] = value;
                            Console.WriteLine($"Witness[{index}].{propertyName} = {value}");
                        }
                    }

                    Console.WriteLine($"Parsed {witnessData.Count} witnesses from form");

                    // Process each witness
                    foreach (var witnessEntry in witnessData.OrderBy(x => x.Key))
                    {
                        var data = witnessEntry.Value;

                        // Skip if name is empty
                        if (!data.ContainsKey("Name") || string.IsNullOrWhiteSpace(data["Name"]))
                        {
                            Console.WriteLine($"Skipping witness {witnessEntry.Key} - no name");
                            continue;
                        }

                        
                        int witnessId = 0;
                        if (data.ContainsKey("WitnessId") && !string.IsNullOrEmpty(data["WitnessId"]))
                        {
                            int.TryParse(data["WitnessId"], out witnessId);
                        }

                        if(witnessId > 0) 
                        { 
                            // Update existing witness
                            var existingWitness = existingCase.Witnesses.FirstOrDefault(w => w.WitnessId == witnessId);
                            if (existingWitness != null)
                            {
                                existingWitness.Name = data["Name"];
                                existingWitness.PhoneNumber = data.ContainsKey("PhoneNumber") ? data["PhoneNumber"] : null;
                                existingWitness.Statement = data.ContainsKey("Statement") ? data["Statement"] : null;
                                Console.WriteLine($"Updated existing witness {witnessId}: {existingWitness.Name}");
                            }
                            else
                            {
                                Console.WriteLine($"⚠ Warning: WitnessId {witnessId} not found in case");
                            }
                        }
                        else
                        {
                            // Add new witness
                            var newWitness = new Witness
                            {
                                Name = data["Name"],
                                PhoneNumber = data.ContainsKey("PhoneNumber") ? data["PhoneNumber"] : null,
                                Statement = data.ContainsKey("Statement") ? data["Statement"] : null,
                                CaseId = model.CaseId
                            };

                            _context.Witnesses.Add(newWitness);
                            Console.WriteLine($"Added new witness: {newWitness.Name}");
                        }
                    }

                    Console.WriteLine("=== Saving to database ===");
                    await _context.SaveChangesAsync();

                    await _activityLogService.LogActivityAsync(
    ActivityActions.CaseUpdated,
    "Case",
    model.CaseId,
    $"Case {model.CaseNumber} updated"
);

                    Console.WriteLine("=== Save completed ===");

                    TempData["SuccessMessage"] = "Case updated successfully";
                    return RedirectToAction("Details", new { id = model.CaseId });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    ModelState.AddModelError("", "Error updating case: " + ex.Message);
                }
            }
            else
            {
                
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

            // Reload witnesses if returning to view
            model.Witnesses = await _context.Witnesses.Where(w => w.CaseId == model.CaseId).ToListAsync();

            return View(model);
        }


    }
}
