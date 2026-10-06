using AutomatedAccountedPayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutomatedAccountedPayroll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "User")]
    public class LeaveBalanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Forbid();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.IdentityUserId == userId &&
                    e.IsActive);

            if (employee == null)
            {
                return View(
                    "~/Areas/Employee/Views/Dashboard/NoEmployee.cshtml");
            }

            var currentYear = DateTime.Now.Year;

            var leaveBalances = await _context.LeaveBalances
                .Where(lb =>
                    lb.EmployeeId == employee.EmployeeId &&
                    lb.Year == currentYear)
                .OrderBy(lb => lb.LeaveType)
                .ToListAsync();

            return View(leaveBalances);
        }
    }
}