using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HEMS.Models;
using HEMS.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Added RoleManager
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Email and Password are required.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);

                // Smart Redirect based on Role
                if (await _userManager.IsInRoleAsync(user, "Student"))
                {
                    return RedirectToAction("Index", "Student");
                }
                else if (await _userManager.IsInRoleAsync(user, "Coordinator"))
                {
                    return RedirectToAction("Index", "Exams");
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid login attempt. Please check your credentials.");
            return View();
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string fullName)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // 1. Ensure the "Student" role exists in the DB
                    if (!await _roleManager.RoleExistsAsync("Student"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Student"));
                    }

                    // 2. Assign the Role (This automates your manual SQL fix)
                    await _userManager.AddToRoleAsync(user, "Student");

                    // 3. Create the linked Student profile record
                    var studentProfile = new Student
                    {
                        UserId = user.Id,
                        FullName = fullName // Ensure your Student model has this property
                    };

                    _context.Students.Add(studentProfile);
                    await _context.SaveChangesAsync();

                    // 4. Sign in and Redirect
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Student");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            // Redirects to the Login action of this controller
            return RedirectToAction("Login", "Account");
        }
    }
}