using AutomatedAccountedPayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutomatedAccountedPayroll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "User")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
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
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e =>
                    e.IdentityUserId == userId &&
                    e.IsActive);

            if (employee == null)
            {
                return View("NoEmployee");
            }

            var employeeId = employee.EmployeeId;

            // =====================================================
            // ATTENDANCE
            // =====================================================

            var today = DateTime.Today;

            var todayAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employeeId &&
                    a.AttendanceDate == today);

            // =====================================================
            // LEAVES
            // =====================================================

            var totalLeaves = await _context.Leaves
                .CountAsync(l =>
                    l.EmployeeId == employeeId);

            var pendingLeaves = await _context.Leaves
                .CountAsync(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == "Pending");

            var approvedLeaves = await _context.Leaves
                .CountAsync(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == "Approved");

            // =====================================================
            // LEAVE BALANCE
            // =====================================================

            var currentYear = DateTime.Now.Year;

            var totalAllocatedLeave = await _context.LeaveBalances
                .Where(lb =>
                    lb.EmployeeId == employeeId &&
                    lb.Year == currentYear)
                .SumAsync(lb => (decimal?)lb.AllocatedDays) ?? 0;

            var totalUsedLeave = await _context.LeaveBalances
                .Where(lb =>
                    lb.EmployeeId == employeeId &&
                    lb.Year == currentYear)
                .SumAsync(lb => (decimal?)lb.UsedDays) ?? 0;

            var remainingLeave =
                totalAllocatedLeave - totalUsedLeave;

            // =====================================================
            // PAYROLL
            // =====================================================

            var payrolls = await _context.Payrolls
                .Where(p =>
                    p.EmployeeId == employeeId)
                .OrderByDescending(p => p.PayrollMonth)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            var totalPayrollRecords = payrolls.Count;

            var latestPayroll = payrolls.FirstOrDefault();

            var latestNetSalary =
                latestPayroll?.NetSalary ?? 0;

            // =====================================================
            // LOANS
            // =====================================================

            var activeLoans = await _context.Loans
                .Where(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == "Active")
                .ToListAsync();

            var activeLoanCount = activeLoans.Count;

            var remainingLoanBalance =
                activeLoans.Sum(l => l.RemainingBalance);

            // =====================================================
            // VIEW DATA
            // =====================================================

            ViewBag.Employee = employee;

            ViewBag.TodayAttendance = todayAttendance;

            ViewBag.TotalLeaves = totalLeaves;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.ApprovedLeaves = approvedLeaves;

            ViewBag.TotalAllocatedLeave =
                totalAllocatedLeave;

            ViewBag.TotalUsedLeave =
                totalUsedLeave;

            ViewBag.RemainingLeave =
                remainingLeave;

            ViewBag.TotalPayrollRecords =
                totalPayrollRecords;

            ViewBag.LatestPayroll =
                latestPayroll;

            ViewBag.LatestNetSalary =
                latestNetSalary;

            ViewBag.ActiveLoanCount =
                activeLoanCount;

            ViewBag.RemainingLoanBalance =
                remainingLoanBalance;

            return View();
        }
    }
}