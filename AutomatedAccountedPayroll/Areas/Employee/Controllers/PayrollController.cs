using AutomatedAccountedPayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Security.Claims;

namespace AutomatedAccountedPayroll.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "User")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollController(ApplicationDbContext context)
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

            var payrolls = await _context.Payrolls
                .Where(p =>
                    p.EmployeeId == employee.EmployeeId)
                .OrderByDescending(p => p.PayrollMonth)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payrolls);
        }

        public async Task<IActionResult> Payslip(int id)
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

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Department)
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Designation)
                .FirstOrDefaultAsync(p =>
                    p.PayrollId == id &&
                    p.EmployeeId == employee.EmployeeId);

            if (payroll == null)
            {
                return NotFound();
            }

            return View(payroll);
        }

        public async Task<IActionResult> DownloadPayslip(int id)
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

            var payroll = await _context.Payrolls
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Department)
                .Include(p => p.Employee)
                .ThenInclude(e => e!.Designation)
                .FirstOrDefaultAsync(p =>
                    p.PayrollId == id &&
                    p.EmployeeId == employee.EmployeeId);

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
                                .Text(
                                    payroll.PayrollMonth
                                        .ToString("MMMM yyyy"))
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

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Employee Code");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.Employee?.EmployeeCode
                                            ?? "N/A");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Employee Name");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.Employee == null
                                                ? "N/A"
                                                : $"{payroll.Employee.FirstName} {payroll.Employee.LastName}");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Department");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.Employee?.Department
                                                ?.DepartmentName
                                            ?? "N/A");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Designation");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.Employee?.Designation
                                                ?.DesignationName
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

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Basic Salary");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.BasicSalary
                                                .ToString("N2"));

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Allowances");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.Allowances
                                                .ToString("N2"));

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Other Deductions");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.Deductions
                                                .ToString("N2"));

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Loan Deduction");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.LoanDeduction
                                                .ToString("N2"));

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Net Salary")
                                        .Bold();

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .AlignRight()
                                        .Text(
                                            payroll.NetSalary
                                                .ToString("N2"))
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

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Status");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.Status ?? "Draft");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Payment Date");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.PaymentDate?
                                                .ToString("dd-MM-yyyy")
                                            ?? "Not Paid");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Payment Method");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.PaymentMethod
                                            ?? "Not Paid");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text("Payment Reference");

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(
                                            payroll.PaymentReference
                                            ?? "N/A");
                                });

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
                                                .Text(
                                                    "Employee Signature");
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
    }
}