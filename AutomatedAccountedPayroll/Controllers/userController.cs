using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private const string DefaultAdminEmail =
            "Admin.example@gmail.com";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        // =========================================================
        // USER LIST
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var userRoles = new Dictionary<string, string>();

            var linkedEmployees = await _context.Employees
                .Where(e => e.IdentityUserId != null)
                .ToDictionaryAsync(
                    e => e.IdentityUserId!,
                    e => e);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userRoles[user.Id] =
                    roles.FirstOrDefault() ?? "User";
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.LinkedEmployees = linkedEmployees;

            return View(users);
        }


        // =========================================================
        // USER DETAILS
        // =========================================================

        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles;

            var linkedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.IdentityUserId == user.Id);

            ViewBag.LinkedEmployee = linkedEmployee;

            return View(user);
        }


        // =========================================================
        // CREATE USER - GET
        // =========================================================

        public IActionResult Create()
        {
            return View();
        }


        // =========================================================
        // CREATE USER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string email,
            string password,
            string role)
        {
            email = email?.Trim() ?? string.Empty;
            role = role?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Password is required.");
            }
            var validRoles = new[] { "Admin", "User", "HR", "Accountant" };

            if (!validRoles.Contains(role))
            {
                ModelState.AddModelError(
                    "role",
                    "Invalid role selected.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "email",
                    "A user with this email already exists.");

                return View();
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                user,
                password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View();
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);

                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    return View();
                }
            }

            var roleAssignment =
                await _userManager.AddToRoleAsync(user, role);

            if (!roleAssignment.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                foreach (var error in roleAssignment.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View();
            }

            TempData["Success"] =
                "User created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT USER - GET
        // =========================================================

        public async Task<IActionResult> Edit(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.CurrentRole =
                roles.FirstOrDefault() ?? "User";

            return View(user);
        }


        // =========================================================
        // EDIT USER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            string email,
            string role)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            email = email?.Trim() ?? string.Empty;
            role = role?.Trim() ?? string.Empty;

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            var currentRole =
                currentRoles.FirstOrDefault() ?? "User";


            // -----------------------------------------------------
            // PROTECT DEFAULT ADMIN
            // -----------------------------------------------------

            if (string.Equals(
                    user.Email,
                    DefaultAdminEmail,
                    StringComparison.OrdinalIgnoreCase) &&
                role != "Admin")
            {
                ModelState.AddModelError(
                    "role",
                    "The default Admin account must remain an Admin.");
            }


            // -----------------------------------------------------
            // VALIDATE ROLE
            // -----------------------------------------------------
            var validRoles = new[] { "Admin", "User", "HR", "Accountant" };

            if (!validRoles.Contains(role))
            {
                ModelState.AddModelError(
                    "role",
                    "Invalid role selected.");
            }


            // -----------------------------------------------------
            // VALIDATE EMAIL
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email is required.");
            }


            // -----------------------------------------------------
            // CHECK DUPLICATE EMAIL
            // -----------------------------------------------------

            var existingEmailUser =
                await _userManager.FindByEmailAsync(email);

            if (existingEmailUser != null &&
                existingEmailUser.Id != user.Id)
            {
                ModelState.AddModelError(
                    "email",
                    "This email is already being used.");
            }


            // -----------------------------------------------------
            // CHECK EMPLOYEE LINK
            // -----------------------------------------------------

            var linkedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.IdentityUserId == user.Id);

            if (linkedEmployee != null &&
                currentRole == "User" &&
                role == "Admin")
            {
                ModelState.AddModelError(
                    "role",
                    "This account is linked to an employee. Unlink the employee account before changing the role to Admin.");
            }


            if (!ModelState.IsValid)
            {
                ViewBag.CurrentRole = currentRole;
                return View(user);
            }


            // -----------------------------------------------------
            // UPDATE EMAIL THROUGH IDENTITY
            // -----------------------------------------------------

            if (!string.Equals(
                    user.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                var emailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        email);

                if (!emailResult.Succeeded)
                {
                    foreach (var error in emailResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    ViewBag.CurrentRole = currentRole;
                    return View(user);
                }
            }


            // -----------------------------------------------------
            // UPDATE USERNAME THROUGH IDENTITY
            // -----------------------------------------------------

            if (!string.Equals(
                    user.UserName,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                var usernameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        email);

                if (!usernameResult.Succeeded)
                {
                    foreach (var error in usernameResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    ViewBag.CurrentRole = currentRole;
                    return View(user);
                }
            }


            // -----------------------------------------------------
            // UPDATE OTHER USER DATA
            // -----------------------------------------------------

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.CurrentRole = currentRole;
                return View(user);
            }


            // -----------------------------------------------------
            // UPDATE ROLE
            // -----------------------------------------------------

            var latestRoles =
                await _userManager.GetRolesAsync(user);

            if (latestRoles.Any())
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        latestRoles);

                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    ViewBag.CurrentRole = currentRole;
                    return View(user);
                }
            }

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    role);

            if (!addRoleResult.Succeeded)
            {
                foreach (var error in addRoleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                ViewBag.CurrentRole = currentRole;
                return View(user);
            }


            TempData["Success"] =
                "User updated successfully.";

            return RedirectToAction(nameof(Index));
        }





        // =========================================================
        // RESET USER PASSWORD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string id,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] =
                    "New password is required.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] =
                    "New password and confirmation password do not match.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user,
                token,
                newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    string.Join(
                        " ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }

            TempData["Success"] =
                "User password reset successfully.";

            return RedirectToAction(
                nameof(Edit),
                new { id });
        }




        // =========================================================
        // DISABLE USER ACCOUNT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableAccount(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Protect the default Admin account
            if (string.Equals(
                    user.Email,
                    DefaultAdminEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The default Admin account cannot be disabled.";

                return RedirectToAction(nameof(Index));
            }

            // Check whether the account is already disabled
            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                TempData["Error"] =
                    "This user account is already disabled.";

                return RedirectToAction(nameof(Index));
            }

            user.LockoutEnd =
                DateTimeOffset.UtcNow.AddYears(10);

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to disable the user account.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "User account disabled successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // ENABLE USER ACCOUNT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAccount(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.LockoutEnd = null;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to enable the user account.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "User account enabled successfully.";

            return RedirectToAction(nameof(Index));
        }




        // =========================================================
        // DELETE USER - GET
        // =========================================================

        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }


        // =========================================================
        // DELETE USER - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // PROTECT DEFAULT ADMIN
            // -----------------------------------------------------

            if (string.Equals(
                    user.Email,
                    DefaultAdminEmail,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "The default Admin account cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // CHECK EMPLOYEE LINK
            // -----------------------------------------------------

            var linkedEmployee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.IdentityUserId == user.Id);

            if (linkedEmployee != null)
            {
                TempData["Error"] =
                    $"This account is linked to employee {linkedEmployee.FirstName} {linkedEmployee.LastName}. Unlink the employee account before deleting this user.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // DELETE USER
            // -----------------------------------------------------

            var result =
                await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Unable to delete the user.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "User deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}