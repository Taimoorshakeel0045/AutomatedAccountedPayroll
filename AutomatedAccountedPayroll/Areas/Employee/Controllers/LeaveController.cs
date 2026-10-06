using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutomatedAccountedPayroll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "User")]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveController(ApplicationDbContext context)
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
                .FirstOrDefaultAsync(e =>
                    e.IdentityUserId == userId &&
                    e.IsActive);

            if (employee == null)
            {
                return View(
                    "~/Areas/Employee/Views/Dashboard/NoEmployee.cshtml");
            }

            var leaves = await _context.Leaves
                .Where(l => l.EmployeeId == employee.EmployeeId)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();

            return View(leaves);
        }

        // =========================================================
        // APPLY FOR LEAVE - GET
        // =========================================================

        public IActionResult Create()
        {
            return View(new Leave
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today
            });
        }

        // =========================================================
        // APPLY FOR LEAVE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LeaveType,StartDate,EndDate,Reason")] Leave leave)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

            if (leave.EndDate < leave.StartDate)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "End date cannot be earlier than start date.");
            }

            if (leave.StartDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "StartDate",
                    "Start date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                return View(leave);
            }

            leave.EmployeeId = employee.EmployeeId;
            leave.TotalDays = (decimal)((leave.EndDate.Date - leave.StartDate.Date).TotalDays + 1);
            leave.Status = "Pending";
            leave.AppliedDate = DateTime.UtcNow;

            _context.Leaves.Add(leave);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request submitted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}