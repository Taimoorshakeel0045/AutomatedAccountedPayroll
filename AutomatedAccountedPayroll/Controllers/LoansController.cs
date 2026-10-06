using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // LOAN LIST
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Employee)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(loans);
        }


        // =========================================================
        // LOAN DETAILS
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LoanId == id);

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
        // CREATE LOAN - GET
        // =========================================================

        public async Task<IActionResult> Create()
        {
            await LoadEmployees();

            return View();
        }


        // =========================================================
        // CREATE LOAN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Loan loan)
        {
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
                    "Monthly installment cannot be greater than the loan amount.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEmployees(loan.EmployeeId);
                return View(loan);
            }

            loan.RemainingBalance = loan.LoanAmount;
            loan.Status = "Active";
            loan.CreatedAt = DateTime.UtcNow;

            _context.Loans.Add(loan);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Loan created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EDIT LOAN - GET
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .FindAsync(id);

            if (loan == null)
                return NotFound();

            await LoadEmployees(loan.EmployeeId);

            return View(loan);
        }


        // =========================================================
        // EDIT LOAN - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Loan loan)
        {
            if (id != loan.LoanId)
            {
                return NotFound();
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
                    "Monthly installment cannot be greater than the loan amount.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEmployees(loan.EmployeeId);
                return View(loan);
            }

            var existingLoan = await _context.Loans
                .FirstOrDefaultAsync(l => l.LoanId == id);

            if (existingLoan == null)
            {
                return NotFound();
            }

            // Get total payments already made for this loan
            decimal totalPaid = await _context.LoanPayments
                .Where(p => p.LoanId == id)
                .SumAsync(p => p.PaymentAmount);

            // New loan amount cannot be less than money already paid
            if (loan.LoanAmount < totalPaid)
            {
                ModelState.AddModelError(
                    "LoanAmount",
                    $"Loan amount cannot be less than the amount already paid ({totalPaid:N2}).");

                await LoadEmployees(loan.EmployeeId);
                return View(loan);
            }

            // Calculate the correct remaining balance
            decimal newRemainingBalance =
                loan.LoanAmount - totalPaid;

            existingLoan.EmployeeId = loan.EmployeeId;
            existingLoan.LoanAmount = loan.LoanAmount;
            existingLoan.MonthlyInstallment = loan.MonthlyInstallment;
            existingLoan.StartDate = loan.StartDate;
            existingLoan.RemainingBalance = newRemainingBalance;

            if (newRemainingBalance == 0)
            {
                existingLoan.Status = "Completed";
            }
            else
            {
                existingLoan.Status = "Active";
            }

            existingLoan.Remarks = loan.Remarks;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Loan updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // ADD LOAN PAYMENT - GET
        // =========================================================

        public async Task<IActionResult> AddPayment(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LoanId == id);

            if (loan == null)
                return NotFound();

            if (loan.RemainingBalance <= 0)
            {
                TempData["Error"] =
                    "This loan has already been fully paid.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = loan.LoanId });
            }

            ViewBag.Loan = loan;

            return View();
        }


        // =========================================================
        // ADD LOAN PAYMENT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(
            int loanId,
            DateTime paymentDate,
            decimal paymentAmount,
            string paymentMethod,
            string? remarks)
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null)
                return NotFound();

            if (loan.RemainingBalance <= 0)
            {
                TempData["Error"] =
                    "This loan has already been fully paid.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = loanId });
            }

            if (paymentAmount <= 0)
            {
                ModelState.AddModelError(
                    "paymentAmount",
                    "Payment amount must be greater than zero.");
            }

            if (paymentAmount > loan.RemainingBalance)
            {
                ModelState.AddModelError(
                    "paymentAmount",
                    $"Payment cannot be greater than the remaining balance ({loan.RemainingBalance:N2}).");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                ModelState.AddModelError(
                    "paymentMethod",
                    "Please select a payment method.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Loan = loan;
                return View();
            }

            decimal newRemainingBalance =
                loan.RemainingBalance - paymentAmount;

            if (newRemainingBalance < 0)
            {
                newRemainingBalance = 0;
            }

            var payment = new LoanPayment
            {
                LoanId = loan.LoanId,
                PaymentDate = paymentDate,
                PaymentAmount = paymentAmount,
                RemainingBalance = newRemainingBalance,
                PaymentMethod = paymentMethod,
                Remarks = remarks,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoanPayments.Add(payment);

            loan.RemainingBalance = newRemainingBalance;

            if (newRemainingBalance == 0)
            {
                loan.Status = "Completed";
            }
            else
            {
                loan.Status = "Active";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Loan payment recorded successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = loan.LoanId });
        }


        // =========================================================
        // DELETE LOAN - GET
        // =========================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var loan = await _context.Loans
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.LoanId == id);

            if (loan == null)
                return NotFound();

            return View(loan);
        }


        // =========================================================
        // DELETE LOAN - POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans
                .FindAsync(id);

            if (loan == null)
                return NotFound();

            _context.Loans.Remove(loan);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Loan deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // EMPLOYEE DROPDOWN
        // =========================================================

        private async Task LoadEmployees(int? selectedEmployeeId = null)
        {
            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewBag.SelectedEmployeeId = selectedEmployeeId;
        }
    }
}