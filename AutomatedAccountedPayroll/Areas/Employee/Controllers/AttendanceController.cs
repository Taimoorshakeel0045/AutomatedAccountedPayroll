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
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
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

            var today = DateTime.Today;

            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate == today);

            var history = await _context.Attendances
                .Where(a =>
                    a.EmployeeId == employee.EmployeeId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            ViewBag.TodayAttendance = todayAttendance;

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn()
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

            var today = DateTime.Today;

            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate == today);

            if (existingAttendance != null)
            {
                TempData["Error"] =
                    "Attendance has already been marked for today.";

                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;

            var attendance = new Attendance
            {
                EmployeeId = employee.EmployeeId,
                AttendanceDate = today,
                Status = "Present",
                CheckInTime = now.TimeOfDay,
                LateMinutes = 0,
                CreatedAt = now
            };

            _context.Attendances.Add(attendance);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Check-in recorded successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
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

            var today = DateTime.Today;

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate == today);

            if (attendance == null)
            {
                TempData["Error"] =
                    "You must check in before checking out.";

                return RedirectToAction(nameof(Index));
            }

            if (!attendance.CheckInTime.HasValue)
            {
                TempData["Error"] =
                    "Check-in time was not found.";

                return RedirectToAction(nameof(Index));
            }

            if (attendance.CheckOutTime.HasValue)
            {
                TempData["Error"] =
                    "You have already checked out today.";

                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;
            var checkOutTime = now.TimeOfDay;

            if (checkOutTime < attendance.CheckInTime.Value)
            {
                TempData["Error"] =
                    "Check-out time cannot be earlier than check-in time.";

                return RedirectToAction(nameof(Index));
            }

            attendance.CheckOutTime = checkOutTime;

            var workingTime =
                checkOutTime - attendance.CheckInTime.Value;

            attendance.WorkingHours =
                Math.Round(
                    (decimal)workingTime.TotalHours,
                    2);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Check-out recorded successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}