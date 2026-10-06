using System.Diagnostics;
using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("HR"))
            {
                return RedirectToAction("Index", "Employees");
            }

            if (User.IsInRole("Accountant"))
            {
                return RedirectToAction("Index", "Payrolls");
            }
            if (User.IsInRole("User"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Employee" });
            }


            var totalEmployees = await _context.Employees.CountAsync();

            var activeEmployees = await _context.Employees
                .CountAsync(e => e.IsActive);

            var totalUsers = await _context.Users.CountAsync();

            var activeLoans = await _context.Loans
                .CountAsync(l => l.Status == "Active");

            var outstandingLoanBalance = await _context.Loans
                .Where(l => l.Status == "Active")
                .SumAsync(l => (decimal?)l.RemainingBalance) ?? 0;

            var totalPayroll = await _context.Payrolls.CountAsync();

            var paidPayroll = await _context.Payrolls
                .CountAsync(p => p.Status == "Paid");

            var pendingLeaves = await _context.Leaves
                .CountAsync(l => l.Status == "Pending");

            var totalNetPayroll = await _context.Payrolls
                .SumAsync(p => (decimal?)p.NetSalary) ?? 0;

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveLoans = activeLoans;
            ViewBag.OutstandingLoanBalance = outstandingLoanBalance;
            ViewBag.TotalPayroll = totalPayroll;
            ViewBag.PaidPayroll = paidPayroll;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.TotalNetPayroll = totalNetPayroll;

            var recentPayrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentLoans = await _context.Loans
                .Include(l => l.Employee)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPayrolls = recentPayrolls;
            ViewBag.RecentLoans = recentLoans;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}