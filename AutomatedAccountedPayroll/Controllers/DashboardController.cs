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

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.InactiveEmployees = inactiveEmployees;
            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.TotalDesignations = totalDesignations;

            return View();
        }
    }
}
