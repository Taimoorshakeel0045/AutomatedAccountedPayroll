using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;

[Authorize(Roles = "Admin,Accountant")]
public class LoanPaymentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LoanPaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // INDEX
    // =========================================================

    public async Task<IActionResult> Index(int? loanId)
    {
        if (loanId == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.LoanId == loanId);

        if (loan == null)
        {
            return NotFound();
        }

        var payments = await _context.LoanPayments
            .Where(p => p.LoanId == loanId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.LoanPaymentId)
            .ToListAsync();

        ViewBag.Loan = loan;

        return View(payments);
    }

    // =========================================================
    // DETAILS
    // =========================================================

    public async Task<IActionResult> Details(int? loanpaymentid)
    {
        if (loanpaymentid == null)
        {
            return NotFound();
        }

        var loanpayment = await _context.LoanPayments
            .Include(p => p.Loan)
            .ThenInclude(l => l!.Employee)
            .FirstOrDefaultAsync(
                p => p.LoanPaymentId == loanpaymentid);

        if (loanpayment == null)
        {
            return NotFound();
        }

        return View(loanpayment);
    }

    // =========================================================
    // CREATE GET
    // =========================================================

    public async Task<IActionResult> Create(int? loanId)
    {
        if (loanId == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.LoanId == loanId);

        if (loan == null)
        {
            return NotFound();
        }

        if (loan.Status != "Active")
        {
            TempData["Error"] =
                "Payments can only be added to active loans.";

            return RedirectToAction(
                nameof(Index),
                new { loanId = loan.LoanId });
        }

        if (loan.RemainingBalance <= 0)
        {
            TempData["Error"] =
                "This loan has no remaining balance.";

            return RedirectToAction(
                nameof(Index),
                new { loanId = loan.LoanId });
        }

        var payment = new LoanPayment
        {
            LoanId = loan.LoanId,
            PaymentDate = DateTime.Today,
            PaymentAmount = Math.Min(
                loan.MonthlyInstallment,
                loan.RemainingBalance),
            PaymentMethod = "Payroll Deduction"
        };

        ViewBag.Loan = loan;

        return View(payment);
    }

    // =========================================================
    // CREATE POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        LoanPayment loanpayment)
    {
        var loan = await _context.Loans
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(
                l => l.LoanId == loanpayment.LoanId);

        if (loan == null)
        {
            return NotFound();
        }

        if (loan.Status != "Active")
        {
            ModelState.AddModelError(
                "",
                "Payments can only be added to active loans.");
        }

        if (loan.RemainingBalance <= 0)
        {
            ModelState.AddModelError(
                "",
                "This loan has no remaining balance.");
        }

        if (loanpayment.PaymentAmount <= 0)
        {
            ModelState.AddModelError(
                "PaymentAmount",
                "Payment amount must be greater than zero.");
        }

        if (loanpayment.PaymentAmount >
            loan.RemainingBalance)
        {
            ModelState.AddModelError(
                "PaymentAmount",
                "Payment amount cannot be greater than the remaining balance.");
        }


        if (loanpayment.PaymentDate > DateTime.Today)
        {
            ModelState.AddModelError(
                "PaymentDate",
                "Payment date cannot be in the future.");
        }




        if (!ModelState.IsValid)
        {
            ViewBag.Loan = loan;
            return View(loanpayment);
        }

        decimal newBalance =
            loan.RemainingBalance -
            loanpayment.PaymentAmount;

        if (newBalance < 0)
        {
            newBalance = 0;
        }

        loanpayment.RemainingBalance = newBalance;
        loanpayment.CreatedAt = DateTime.UtcNow;

        loan.RemainingBalance = newBalance;

        if (newBalance == 0)
        {
            loan.Status = "Completed";
        }

        _context.LoanPayments.Add(loanpayment);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Loan payment recorded successfully.";

        return RedirectToAction(
            nameof(Index),
            new { loanId = loan.LoanId });
    }

    // =========================================================
    // EDIT GET
    // =========================================================

    public async Task<IActionResult> Edit(
        int? loanpaymentid)
    {
        if (loanpaymentid == null)
        {
            return NotFound();
        }

        var loanpayment = await _context.LoanPayments
            .Include(p => p.Loan)
            .ThenInclude(l => l!.Employee)
            .FirstOrDefaultAsync(
                p => p.LoanPaymentId == loanpaymentid);

        if (loanpayment == null)
        {
            return NotFound();
        }

        ViewBag.Loan = loanpayment.Loan;

        return View(loanpayment);
    }

    // =========================================================
    // EDIT POST
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int loanpaymentid,
        LoanPayment loanpayment)
    {
        if (loanpaymentid !=
            loanpayment.LoanPaymentId)
        {
            return NotFound();
        }

        var existingPayment =
            await _context.LoanPayments
                .FirstOrDefaultAsync(
                    p => p.LoanPaymentId ==
                         loanpaymentid);

        if (existingPayment == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(
                l => l.LoanId ==
                     existingPayment.LoanId);

        if (loan == null)
        {
            return NotFound();
        }

        if (loanpayment.PaymentAmount <= 0)
        {
            ModelState.AddModelError(
                "PaymentAmount",
                "Payment amount must be greater than zero.");
        }

        // Restore the balance before the old payment
        decimal balanceBeforeOldPayment =
            loan.RemainingBalance +
            existingPayment.PaymentAmount;

        if (loanpayment.PaymentAmount >
            balanceBeforeOldPayment)
        {
            ModelState.AddModelError(
                "PaymentAmount",
                "Payment amount cannot be greater than the available balance.");
        }

        if (!ModelState.IsValid)
        {
            loanpayment.LoanId =
                existingPayment.LoanId;

            ViewBag.Loan = loan;

            return View(loanpayment);
        }

        decimal newBalance =
            balanceBeforeOldPayment -
            loanpayment.PaymentAmount;

        if (newBalance < 0)
        {
            newBalance = 0;
        }

        existingPayment.PaymentDate =
            loanpayment.PaymentDate;

        existingPayment.PaymentAmount =
            loanpayment.PaymentAmount;

        existingPayment.PaymentMethod =
            loanpayment.PaymentMethod;

        existingPayment.Remarks =
            loanpayment.Remarks;

        existingPayment.RemainingBalance =
            newBalance;

        loan.RemainingBalance =
            newBalance;

        if (newBalance == 0)
        {
            loan.Status = "Completed";
        }
        else
        {
            loan.Status = "Active";
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Loan payment updated successfully.";

        return RedirectToAction(
            nameof(Index),
            new { loanId = loan.LoanId });
    }

    // =========================================================
    // DELETE GET
    // =========================================================

    public async Task<IActionResult> Delete(
        int? loanpaymentid)
    {
        if (loanpaymentid == null)
        {
            return NotFound();
        }

        var loanpayment = await _context.LoanPayments
            .Include(p => p.Loan)
            .ThenInclude(l => l!.Employee)
            .FirstOrDefaultAsync(
                p => p.LoanPaymentId ==
                     loanpaymentid);

        if (loanpayment == null)
        {
            return NotFound();
        }

        return View(loanpayment);
    }

    // =========================================================
    // DELETE POST
    // =========================================================

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int loanpaymentid)
    {
        var loanpayment =
            await _context.LoanPayments
                .FirstOrDefaultAsync(
                    p => p.LoanPaymentId ==
                         loanpaymentid);

        if (loanpayment == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .FirstOrDefaultAsync(
                l => l.LoanId ==
                     loanpayment.LoanId);

        if (loan == null)
        {
            return NotFound();
        }

        // Restore the amount that was paid
        loan.RemainingBalance +=
            loanpayment.PaymentAmount;

        if (loan.RemainingBalance >
            loan.LoanAmount)
        {
            loan.RemainingBalance =
                loan.LoanAmount;
        }

        if (loan.RemainingBalance > 0)
        {
            loan.Status = "Active";
        }
        else
        {
            loan.Status = "Completed";
        }

        _context.LoanPayments.Remove(
            loanpayment);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Loan payment deleted and loan balance restored.";

        return RedirectToAction(
            nameof(Index),
            new { loanId = loan.LoanId });
    }
}