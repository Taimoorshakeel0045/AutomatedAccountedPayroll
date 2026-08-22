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


namespace AutomatedAccountedPayroll.Controllers
{
    public class PayrollsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollsController(ApplicationDbContext context)
        {
            _context = context;
        }
        //Payslip Action starts

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

        // Payslip Action End

        // pdf generation start
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
                                        .Text(payroll.Employee?.EmployeeCode ?? "N/A");

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
                                        .Text(payroll.BasicSalary.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Allowances");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(payroll.Allowances.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Deductions");

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(payroll.Deductions.ToString("N2"));

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Net Salary")
                                        .Bold();

                                    table.Cell().Border(1).Padding(5)
                                        .AlignRight()
                                        .Text(payroll.NetSalary.ToString("N2"))
                                        .Bold();
                                });

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
                                        .Text(payroll.Status ?? "Draft");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Payment Date");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(
                                            payroll.PaymentDate?.ToString("dd-MM-yyyy")
                                            ?? "Not Paid");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Payment Method");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(payroll.PaymentMethod ?? "Not Paid");

                                    table.Cell().Border(1).Padding(5)
                                        .Text("Payment Reference");

                                    table.Cell().Border(1).Padding(5)
                                        .Text(payroll.PaymentReference ?? "N/A");
                                });

                            column.Item()
                                .PaddingTop(60)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("________________________");

                                            c.Item()
                                                .Text("Employee Signature");
                                        });

                                    row.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("________________________");

                                            c.Item()
                                                .Text("Authorized Signature");
                                        });
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Generated by Automated Accounted Payroll");
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"Payslip-{payroll.Employee?.EmployeeCode ?? payroll.PayrollId.ToString()}-{payroll.PayrollMonth:yyyy-MM}.pdf");
        }
        //pdf generation end 



        // GET: Payrolls
        // pay roll month filter has addedd here 

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



        // GET: Payrolls/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(m => m.PayrollId == id);
            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        // GET: Payrolls/Create
        public IActionResult Create()
        {
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeCode");
            return View();
        }

        // POST: Payrolls/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    [Bind("PayrollId,EmployeeId,PayrollMonth,BasicSalary,Allowances,Deductions,NetSalary,CreatedAt")] Payroll payroll)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == payroll.EmployeeId);

            if (employee == null)
            {
                ModelState.AddModelError("EmployeeId", "Selected employee was not found.");
            }
            else
            {
                payroll.BasicSalary = employee.BasicSalary;

                payroll.NetSalary =
                    payroll.BasicSalary +
                    payroll.Allowances -
                    payroll.Deductions;
            }

            payroll.PayrollMonth = new DateTime(
    payroll.PayrollMonth.Year,
    payroll.PayrollMonth.Month,
    1);


            //Payroll Duplication Prevention Code Starts 

            var payrollExists = await _context.Payrolls
    .AnyAsync(p =>
        p.EmployeeId == payroll.EmployeeId &&
        p.PayrollMonth.Year == payroll.PayrollMonth.Year &&
        p.PayrollMonth.Month == payroll.PayrollMonth.Month);

            if (payrollExists)
            {
                ModelState.AddModelError(
                    "PayrollMonth",
                    "Payroll for this employee and month already exists.");
            }

            //Payroll Duplication Prevention Code end

            if (ModelState.IsValid)
            {
                _context.Add(payroll);
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


        //Auto Employee Drop-down 
        //this method load the employee basic salary automatically 

[HttpGet]
public async Task<IActionResult> GetEmployeeSalary(int employeeId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            return Json(new
            {
                basicSalary = employee.BasicSalary
            });
        }


        // GET: Payrolls/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll == null)
            {
                return NotFound();
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "EmployeeId", "EmployeeCode", payroll.EmployeeId);
            return View(payroll);
        }

        // POST: Payrolls/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // the new payment info section added in here ( status , p.date , p.method ,p.reffrence )
        //payment status validation here also 

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(
    int id,
    [Bind("PayrollId,EmployeeId,PayrollMonth,BasicSalary,Allowances,Deductions,NetSalary,Status,PaymentDate,PaymentMethod,PaymentReference,CreatedAt")]
    Payroll payroll)
        {
            if (id != payroll.PayrollId)
            {
                return NotFound();
            }

            //validation code start

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

            //validation code end

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payroll);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PayrollExists(payroll.PayrollId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["EmployeeId"] = new SelectList(
                _context.Employees,
                "EmployeeId",
                "EmployeeCode",
                payroll.EmployeeId);

            return View(payroll);
        }


        // GET: Payrolls/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(m => m.PayrollId == id);
            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        // POST: Payrolls/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id);
            if (payroll != null)
            {
                _context.Payrolls.Remove(payroll);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PayrollExists(int id)
        {
            return _context.Payrolls.Any(e => e.PayrollId == id);
        }
    }
}
