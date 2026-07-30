using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPS_ICMS.Data;
using SAPS_ICMS.Models;
using System.Diagnostics;

namespace SAPS_ICMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<Officer> _userManager;
        private readonly SignInManager<Officer> _signInManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<Officer> userManager, SignInManager<Officer> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Welcome()
        {
            // If user is logged in, redirect to dashboard
            if (_signInManager.IsSignedIn(User))
            {
                var officer = await _userManager.GetUserAsync(User);
                return RedirectToAction("Index", "Home");
            }

            // Otherwise, show the welcome page
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var officer = await _userManager.GetUserAsync(User);
          
            return View(officer);
        }

    }
}
