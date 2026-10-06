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
    public class LoanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoanController(ApplicationDbContext context)
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

            var loans = await _context.Loans
                .Where(l => l.EmployeeId == employee.EmployeeId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(loans);
        }

        public async Task<IActionResult> Details(int id)
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

            var loan = await _context.Loans
                .FirstOrDefaultAsync(l =>
                    l.LoanId == id &&
                    l.EmployeeId == employee.EmployeeId);

            if (loan == null)
            {
                return NotFound();
            }

            var payments = await _context.LoanPayments
                .Where(p => p.LoanId == loan.LoanId)
                .OrderByDescending(p => p.PaymentDate)
                .ThenByDescending(p => p.LoanPaymentId)
                .ToListAsync();

            ViewBag.TotalPaid = payments.Sum(p => p.PaymentAmount);
            ViewBag.PaymentCount = payments.Count;
            ViewBag.Payments = payments;

            return View(loan);
        }

        // =========================================================
        // APPLY FOR LOAN - GET
        // =========================================================

        public IActionResult Create()
        {
            return View(new Loan
            {
                StartDate = DateTime.Today
            });
        }

        // =========================================================
        // APPLY FOR LOAN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LoanAmount,MonthlyInstallment,StartDate,Remarks")] Loan loan)
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

            if (loan.LoanAmount <= 0)
            {
                ModelState.AddModelError(
                    "LoanAmount",
                    "Loan amount must be greater than zero.");
            }

            if (loan.MonthlyInstallment <= 0)
            {
                ModelState.AddModelError(
                    "MonthlyInstallment",
                    "Monthly installment must be greater than zero.");
            }

            if (loan.MonthlyInstallment > loan.LoanAmount)
            {
                ModelState.AddModelError(
                    "MonthlyInstallment",
                    "Monthly installment cannot exceed the loan amount.");
            }

            // Prevent multiple pending applications
            var hasPending = await _context.Loans
                .AnyAsync(l =>
                    l.EmployeeId == employee.EmployeeId &&
                    l.Status == "Pending");

            if (hasPending)
            {
                ModelState.AddModelError(
                    "",
                    "You already have a pending loan application. Please wait for a decision.");
            }

            if (!ModelState.IsValid)
            {
                return View(loan);
            }

            loan.EmployeeId = employee.EmployeeId;
            loan.RemainingBalance = loan.LoanAmount;
            loan.Status = "Pending";
            loan.CreatedAt = DateTime.UtcNow;

            _context.Loans.Add(loan);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Loan application submitted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}