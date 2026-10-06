using AutomatedAccountedPayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutomatedAccountedPayroll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "User")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e =>
                    e.IdentityUserId == userId &&
                    e.IsActive);

            if (employee == null)
            {
                return View("~/Areas/Employee/Views/Dashboard/NoEmployee.cshtml");
            }

            return View(employee);
        }
    }
}