using Azure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPS_ICMS.Models;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly UserManager<Officer> _userManager;
    private readonly SignInManager<Officer> _signInManager;

    public AccountController(UserManager<Officer> userManager, SignInManager<Officer> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register() 
    {
        
        return View(); 
    }

    [HttpPost]
    public async Task<IActionResult> Register(string name, string surname, string securityNumber, string password, string role)
    {
        var validRoles = new string[] { Roles.Admin, Roles.Detective, Roles.Investigator };
        if(string.IsNullOrEmpty(role) || !validRoles.Contains(role))
        {
            ModelState.AddModelError("", "Invalid position selected");
            ViewBag.Roles = validRoles.ToList();
            return View();
        }
        var officer = new Officer
        {
            
            UserName = securityNumber,
            SecurityNumber = securityNumber,
            Name = name,
            Surname = surname
            
        };

        var result = await _userManager.CreateAsync(officer, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(officer, role);

            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("Login", "Account");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View();
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(string securityNumber, string password)
    {
        var officer = await _userManager.Users.FirstOrDefaultAsync(u => u.SecurityNumber == securityNumber);
        if (officer == null)
        {
            ModelState.AddModelError("", "Invalid login attempt.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(officer, password, false, false);
        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError("", "Invalid login attempt.");
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Welcome", "Home");
    }
}
