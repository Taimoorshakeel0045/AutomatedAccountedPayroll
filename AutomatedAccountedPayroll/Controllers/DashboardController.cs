using AutomatedAccountedPayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalEmployees = await _context.Employees.CountAsync();

            var activeEmployees = await _context.Employees
                .CountAsync(e => e.IsActive);

            var inactiveEmployees = await _context.Employees
                .CountAsync(e => !e.IsActive);

            var totalDepartments = await _context.Departments.CountAsync();

            var totalDesignations = await _context.Designations.CountAsync();

            // Today's Attendance
            var today = DateTime.Today;

            var todayAttendance = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == today)
                .ToListAsync();

            var todayPresent = todayAttendance.Count(a => a.Status == "Present");
            var todayAbsent = todayAttendance.Count(a => a.Status == "Absent");
            var todayLate = todayAttendance.Count(a => a.Status == "Late");
            var todayLeave = todayAttendance.Count(a => a.Status == "Leave");
            var todayHalfDay = todayAttendance.Count(a => a.Status == "Half Day");

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.InactiveEmployees = inactiveEmployees;
            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.TotalDesignations = totalDesignations;

            ViewBag.TodayPresent = todayPresent;
            ViewBag.TodayAbsent = todayAbsent;
            ViewBag.TodayLate = todayLate;
            ViewBag.TodayLeave = todayLeave;
            ViewBag.TodayHalfDay = todayHalfDay;



            var totalLeaveRequests = await _context.Leaves.CountAsync();

            var approvedLeaves = await _context.Leaves
                .CountAsync(l => l.Status == "Approved");

            var pendingLeaves = await _context.Leaves
                .CountAsync(l => l.Status == "Pending");

            var rejectedLeaves = await _context.Leaves
                .CountAsync(l => l.Status == "Rejected");

            ViewBag.TotalLeaveRequests = totalLeaveRequests;
            ViewBag.ApprovedLeaves = approvedLeaves;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.RejectedLeaves = rejectedLeaves;





            // PAYROLL SUMMARY
            var totalPayrollRecords = await _context.Payrolls.CountAsync();

            var paidPayroll = await _context.Payrolls
                .CountAsync(p => p.Status == "Paid");

            var pendingPayroll = await _context.Payrolls
                .CountAsync(p => p.Status != "Paid" && p.Status != "Processed");

            var totalNetSalary = await _context.Payrolls
                .SumAsync(p => (decimal?)p.NetSalary) ?? 0;

            ViewBag.TotalPayrollRecords = totalPayrollRecords;
            ViewBag.PaidPayroll = paidPayroll;
            ViewBag.PendingPayroll = pendingPayroll;
            ViewBag.TotalNetSalary = totalNetSalary;




            // RECENT PAYROLL ACTIVITY
            var recentPayrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPayrolls = recentPayrolls;

            return View();
        }
    }
}
