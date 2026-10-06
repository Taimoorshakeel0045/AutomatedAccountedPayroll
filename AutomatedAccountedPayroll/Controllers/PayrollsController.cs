using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class PayrollsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PAYSLIP
        // =========================================================

        public async Task<IActionResult> Payslip(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Department)
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Designation)
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        // =========================================================
        // PAYSLIP PDF
        // =========================================================

        public async Task<IActionResult> DownloadPayslip(int id)
        {
            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Department)
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Designation)
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header()
                        .AlignCenter()
                        .Text("AUTOMATED ACCOUNTED PAYROLL")
                        .Bold()
                        .FontSize(20);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item()
                                .AlignCenter()
                                .Text("EMPLOYEE PAYSLIP")
                                .Bold()
                                .FontSize(18);

                            column.Item()
                                .AlignCenter()
                                .Text(payroll.PayrollMonth.ToString("MMMM yyyy"))
                                .FontSize(12);

                            // Employee Information
                            column.Item()
                                .PaddingTop(15)
                                .Text("Employee Information")
                                .Bold()
                                .FontSize(14);

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Employee Code");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.Employee?.EmployeeCode
                                            ?? "N/A");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Employee Name");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.Employee == null
                                                ? "N/A"
                                                : $"{payroll.Employee.FirstName} {payroll.Employee.LastName}");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Department");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.Employee?.Department?.DepartmentName
                                            ?? "N/A");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Designation");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.Employee?.Designation?.DesignationName
                                            ?? "N/A");
                                });

                            // Salary Details
                            column.Item()
                                .PaddingTop(15)
                                .Text("Salary Details")
                                .Bold()
                                .FontSize(14);

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Basic Salary");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.BasicSalary.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Allowances");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.Allowances.ToString("N2"));

                                    // Overtime
                                    table.Cell().Border(1).Padding(5)
                                        .Text("Overtime");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.Overtime.ToString("N2"));

                                    // Bonus
                                    table.Cell().Border(1).Padding(5)
                                        .Text("Bonus");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.Bonus.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Other Deductions");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.Deductions.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Loan Deduction");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.LoanDeduction.ToString("N2"));

                                    // Advance Salary
                                    table.Cell().Border(1).Padding(5)
                                        .Text("Advance Salary");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.AdvanceSalary.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Net Salary")
                                        .Bold();

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.NetSalary.ToString("N2"))
                                        .Bold();
                                });

                            // Payment Information
                            column.Item()
                                .PaddingTop(15)
                                .Text("Payment Information")
                                .Bold()
                                .FontSize(14);

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Status");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.Status ?? "Draft");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Payment Date");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.PaymentDate?
                                                .ToString("dd-MM-yyyy")
                                            ?? "Not Paid");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Payment Method");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.PaymentMethod
                                            ?? "Not Paid");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Payment Reference");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.PaymentReference
                                            ?? "N/A");
                                });

                            // Signatures
                            column.Item()
                                .PaddingTop(60)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text(
                                                    "________________________");

                                            c.Item()
                                                .Text("Employee Signature");
                                        });

                                    row.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text(
                                                    "________________________");

                                            c.Item()
                                                .Text(
                                                    "Authorized Signature");
                                        });
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(
                            "Generated by Automated Accounted Payroll");
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"Payslip-{payroll.Employee?.EmployeeCode ?? payroll.PayrollId.ToString()}-{payroll.PayrollMonth:yyyy-MM}.pdf");
        }

        // =========================================================
        // INDEX
        // =========================================================

        public async Task<IActionResult> Index(DateTime? payrollMonth)
        {
            var payrolls = _context.Payrolls
                .Include(p => p.Employee)
                .AsQueryable();

            if (payrollMonth.HasValue)
            {
                payrolls = payrolls.Where(p =>
                    p.PayrollMonth.Year == payrollMonth.Value.Year &&
                    p.PayrollMonth.Month == payrollMonth.Value.Month);
            }

            ViewData["PayrollMonth"] = payrollMonth;

            return View(await payrolls.ToListAsync());
        }

        // =========================================================
        // DETAILS
        // =========================================================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(
                    p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        // =========================================================
        // CREATE GET
        // =========================================================




        public IActionResult Create()
        {
            ViewData["EmployeeId"] = new SelectList(
                _context.Employees,
                "EmployeeId",
                "EmployeeCode");

            return View(new Payroll
            {
                CreatedAt = DateTime.Now,
                PayrollMonth = DateTime.Now
            });
        }







        // =========================================================
        // CREATE POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "PayrollId,EmployeeId,PayrollMonth,BasicSalary,Allowances,Overtime,Bonus,Deductions,LoanDeduction,AdvanceSalary,NetSalary,CreatedAt")]
            Payroll payroll)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.EmployeeId == payroll.EmployeeId);

            if (employee == null)
            {
                ModelState.AddModelError(
                    "EmployeeId",
                    "Selected employee was not found.");
            }
            else
            {
                // Load basic salary automatically
                payroll.BasicSalary = employee.BasicSalary;

                // Get active loan
                var activeLoan = await _context.Loans
                    .Where(l =>
                        l.EmployeeId == payroll.EmployeeId &&
                        l.Status == "Active" &&
                        l.RemainingBalance > 0)
                    .OrderBy(l => l.StartDate)
                    .FirstOrDefaultAsync();

                // Automatically calculate loan deduction
                if (activeLoan != null)
                {
                    payroll.LoanDeduction = Math.Min(
                        activeLoan.MonthlyInstallment,
                        activeLoan.RemainingBalance);
                }
                else
                {
                    payroll.LoanDeduction = 0;
                }

                // Calculate net salary
                payroll.NetSalary =
                    payroll.BasicSalary +
                    payroll.Allowances +
                    payroll.Overtime +
                    payroll.Bonus -
                    payroll.Deductions -
                    payroll.LoanDeduction -
                    payroll.AdvanceSalary;
            }

            // Payroll business-rule validation
            if (payroll.NetSalary < 0)
            {
                ModelState.AddModelError(
                    "NetSalary",
                    "Net salary cannot be negative.");
            }

            // Store payroll month as first day of month
            payroll.PayrollMonth = new DateTime(
                payroll.PayrollMonth.Year,
                payroll.PayrollMonth.Month,
                1);

            // Payroll duplication prevention
            var payrollExists = await _context.Payrolls
                .AnyAsync(p =>
                    p.EmployeeId == payroll.EmployeeId &&
                    p.PayrollMonth.Year ==
                        payroll.PayrollMonth.Year &&
                    p.PayrollMonth.Month ==
                        payroll.PayrollMonth.Month);

            if (payrollExists)
            {
                ModelState.AddModelError(
                    "PayrollMonth",
                    "Payroll for this employee and month already exists.");
            }

            if (ModelState.IsValid)
            {

                // default creat time 

                payroll.Status = "Draft";

                if (payroll.CreatedAt == default)
                {
                    payroll.CreatedAt = DateTime.UtcNow;
                }






                _context.Payrolls.Add(payroll);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["EmployeeId"] = new SelectList(
                _context.Employees,
                "EmployeeId",
                "EmployeeCode",
                payroll.EmployeeId);

            return View(payroll);
        }

        // =========================================================
        // EMPLOYEE SALARY + LOAN
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetEmployeeSalary(
            int employeeId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(
                    e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            var activeLoan = await _context.Loans
                .Where(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == "Active" &&
                    l.RemainingBalance > 0)
                .OrderBy(l => l.StartDate)
                .FirstOrDefaultAsync();

            decimal loanDeduction = 0;

            if (activeLoan != null)
            {
                loanDeduction = Math.Min(
                    activeLoan.MonthlyInstallment,
                    activeLoan.RemainingBalance);
            }

            return Json(new
            {
                basicSalary = employee.BasicSalary,
                loanDeduction = loanDeduction,
                hasActiveLoan = activeLoan != null
            });
        }

        // =========================================================
        // EDIT GET
        // =========================================================

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .FindAsync(id);

            if (payroll == null)
            {
                return NotFound();
            }

            if (payroll.Status == "Paid")
            {
                TempData["Error"] =
                    "Paid payroll cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            ViewData["EmployeeId"] = new SelectList(
                _context.Employees,
                "EmployeeId",
                "EmployeeCode",
                payroll.EmployeeId);

            return View(payroll);
        }

        // =========================================================
        // EDIT POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
     int id,
     [Bind(
        "PayrollId,EmployeeId,PayrollMonth,BasicSalary,Allowances,Overtime,Bonus,Deductions,LoanDeduction,AdvanceSalary,NetSalary,CreatedAt,Status,PaymentDate,PaymentMethod,PaymentReference")]
    Payroll payroll)
        {
            if (id != payroll.PayrollId)
            {
                return NotFound();
            }

            var existingPayroll = await _context.Payrolls
                .FirstOrDefaultAsync(p => p.PayrollId == id);

            if (existingPayroll == null)
            {
                return NotFound();
            }

            // Paid payroll cannot be edited
            if (existingPayroll.Status == "Paid")
            {
                TempData["Error"] =
                    "Paid payroll cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            // Validate status
            var allowedStatuses = new[]
            {
        "Draft",
        "Processed",
        "Paid"
    };

            if (!allowedStatuses.Contains(payroll.Status))
            {
                ModelState.AddModelError(
                    "Status",
                    "Invalid payroll status.");
            }
            else
            {
                bool validTransition =
                    existingPayroll.Status == "Draft" &&
                    (payroll.Status == "Draft" ||
                     payroll.Status == "Processed")
                    ||
                    existingPayroll.Status == "Processed" &&
                    (payroll.Status == "Processed" ||
                     payroll.Status == "Paid");

                if (!validTransition)
                {
                    ModelState.AddModelError(
                        "Status",
                        $"Invalid status transition from {existingPayroll.Status} to {payroll.Status}.");
                }
            }

            // Paid status requires payment information
            if (payroll.Status == "Paid")
            {
                if (!payroll.PaymentDate.HasValue)
                {
                    ModelState.AddModelError(
                        "PaymentDate",
                        "Payment date is required when payroll is marked as Paid.");
                }

                if (string.IsNullOrWhiteSpace(payroll.PaymentMethod))
                {
                    ModelState.AddModelError(
                        "PaymentMethod",
                        "Payment method is required when payroll is marked as Paid.");
                }

                if (string.IsNullOrWhiteSpace(payroll.PaymentReference))
                {
                    ModelState.AddModelError(
                        "PaymentReference",
                        "Payment reference is required when payroll is marked as Paid.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["EmployeeId"] = new SelectList(
                    _context.Employees,
                    "EmployeeId",
                    "EmployeeCode",
                    payroll.EmployeeId);

                return View(payroll);
            }

            try
            {
                bool isNewlyPaid =
                    existingPayroll.Status != "Paid" &&
                    payroll.Status == "Paid";

                // Process loan deduction when payroll becomes Paid
                if (isNewlyPaid && payroll.LoanDeduction > 0)
                {
                    var activeLoan = await _context.Loans
                        .Where(l =>
                            l.EmployeeId == payroll.EmployeeId &&
                            l.Status == "Active" &&
                            l.RemainingBalance > 0)
                        .OrderBy(l => l.StartDate)
                        .FirstOrDefaultAsync();

                    if (activeLoan != null)
                    {
                        decimal paymentAmount = Math.Min(
                            payroll.LoanDeduction,
                            activeLoan.RemainingBalance);

                        if (paymentAmount > 0)
                        {
                            decimal newBalance =
                                activeLoan.RemainingBalance -
                                paymentAmount;

                            if (newBalance < 0)
                            {
                                newBalance = 0;
                            }

                            var payrollReference =
                                $"Payroll Deduction - Payroll #{payroll.PayrollId}";

                            var existingLoanPayment =
                                await _context.LoanPayments
                                    .FirstOrDefaultAsync(p =>
                                        p.Remarks == payrollReference);

                            if (existingLoanPayment == null)
                            {
                                var loanPayment = new LoanPayment
                                {
                                    LoanId = activeLoan.LoanId,

                                    PaymentDate =
                                        payroll.PaymentDate
                                        ?? DateTime.Today,

                                    PaymentAmount =
                                        paymentAmount,

                                    RemainingBalance =
                                        newBalance,

                                    PaymentMethod =
                                        "Payroll Deduction",

                                    Remarks =
                                        payrollReference,

                                    CreatedAt =
                                        DateTime.UtcNow
                                };

                                _context.LoanPayments.Add(loanPayment);

                                activeLoan.RemainingBalance =
                                    newBalance;

                                if (newBalance == 0)
                                {
                                    activeLoan.Status = "Completed";
                                }
                            }

                            payroll.LoanDeduction = paymentAmount;
                        }
                    }
                    else
                    {
                        payroll.LoanDeduction = 0;
                    }
                }

                // Recalculate Net Salary on server
                payroll.NetSalary =
                    payroll.BasicSalary +
                    payroll.Allowances +
                    payroll.Overtime +
                    payroll.Bonus -
                    payroll.Deductions -
                    payroll.LoanDeduction -
                    payroll.AdvanceSalary;

                if (payroll.NetSalary < 0)
                {
                    ModelState.AddModelError(
                        "NetSalary",
                        "Net salary cannot be negative.");

                    ViewData["EmployeeId"] = new SelectList(
                        _context.Employees,
                        "EmployeeId",
                        "EmployeeCode",
                        payroll.EmployeeId);

                    return View(payroll);
                }

                // Update existing payroll
                existingPayroll.EmployeeId =
                    payroll.EmployeeId;

                existingPayroll.PayrollMonth =
                    new DateTime(
                        payroll.PayrollMonth.Year,
                        payroll.PayrollMonth.Month,
                        1);

                existingPayroll.BasicSalary =
                    payroll.BasicSalary;

                existingPayroll.Allowances =
                    payroll.Allowances;

                existingPayroll.Overtime =
                    payroll.Overtime;

                existingPayroll.Bonus =
                    payroll.Bonus;

                existingPayroll.Deductions =
                    payroll.Deductions;

                existingPayroll.LoanDeduction =
                    payroll.LoanDeduction;

                existingPayroll.AdvanceSalary =
                    payroll.AdvanceSalary;

                existingPayroll.NetSalary =
                    payroll.NetSalary;

                // IMPORTANT
                // Save the new status
                existingPayroll.Status =
                    payroll.Status;

                existingPayroll.PaymentDate =
                    payroll.PaymentDate;

                existingPayroll.PaymentMethod =
                    payroll.PaymentMethod;

                existingPayroll.PaymentReference =
                    payroll.PaymentReference;

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PayrollExists(payroll.PayrollId))
                {
                    return NotFound();
                }

                throw;
            }
        }

        // =========================================================
        // DELETE GET
        // =========================================================

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(
                    p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        // =========================================================
        // DELETE POST
        // =========================================================

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payroll = await _context.Payrolls
                .FirstOrDefaultAsync(
                    p => p.PayrollId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            // Paid payroll cannot be deleted
            if (payroll.Status == "Paid")
            {
                TempData["Error"] =
                    "Paid payroll cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            _context.Payrolls.Remove(payroll);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // PAYROLL EXISTS
        // =========================================================

        private bool PayrollExists(int id)
        {
            return _context.Payrolls
                .Any(e => e.PayrollId == id);
        }
    }
}