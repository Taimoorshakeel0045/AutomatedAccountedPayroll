using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin,HR,Accountant")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // ATTENDANCE REPORT
        // =========================================================

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Attendance(
            DateTime? fromDate, 
            DateTime? toDate,
            int? employeeId,
            string? status)
        {
            var attendances = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                attendances = attendances.Where(a =>
                    a.AttendanceDate.Date >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date;

                attendances = attendances.Where(a =>
                    a.AttendanceDate.Date <= endDate);
            }

            if (employeeId.HasValue)
            {
                attendances = attendances.Where(a =>
                    a.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                attendances = attendances.Where(a =>
                    a.Status == status);
            }

            var attendanceList = await attendances
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee!.FirstName)
                .ThenBy(a => a.Employee!.LastName)
                .ToListAsync();

            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.EmployeeId = employeeId;
            ViewBag.Status = status;

            ViewBag.TotalAttendance = attendanceList.Count;

            ViewBag.PresentCount =
                attendanceList.Count(a => a.Status == "Present");

            ViewBag.AbsentCount =
                attendanceList.Count(a => a.Status == "Absent");

            ViewBag.LateCount =
                attendanceList.Count(a => a.Status == "Late");

            ViewBag.LeaveCount =
                attendanceList.Count(a => a.Status == "Leave");

            ViewBag.HalfDayCount =
                attendanceList.Count(a => a.Status == "Half Day");

            return View(attendanceList);
        }


        // =========================================================
        // ATTENDANCE PDF
        // =========================================================

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> AttendancePdf(
             DateTime? fromDate,
             DateTime? toDate,
            int? employeeId,
            string? status)
        {
            var attendances = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                var startDate = fromDate.Value.Date;

                attendances = attendances.Where(a =>
                    a.AttendanceDate.Date >= startDate);
            }

            if (toDate.HasValue)
            {
                var endDate = toDate.Value.Date;

                attendances = attendances.Where(a =>
                    a.AttendanceDate.Date <= endDate);
            }

            if (employeeId.HasValue)
            {
                attendances = attendances.Where(a =>
                    a.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                attendances = attendances.Where(a =>
                    a.Status == status);
            }

            var attendanceList = await attendances
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee!.FirstName)
                .ThenBy(a => a.Employee!.LastName)
                .ToListAsync();

            var totalRecords = attendanceList.Count;

            var presentCount =
                attendanceList.Count(a => a.Status == "Present");

            var absentCount =
                attendanceList.Count(a => a.Status == "Absent");

            var lateCount =
                attendanceList.Count(a => a.Status == "Late");

            var leaveCount =
                attendanceList.Count(a => a.Status == "Leave");

            var halfDayCount =
                attendanceList.Count(a => a.Status == "Half Day");

            var employeeName = "All Employees";

            if (employeeId.HasValue)
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.EmployeeId == employeeId.Value);

                if (employee != null)
                {
                    employeeName =
                        $"{employee.FirstName} {employee.LastName}";
                }
            }

            var dateRange =
                $"{(fromDate.HasValue ? fromDate.Value.ToString("dd MMM yyyy") : "All")} - " +
                $"{(toDate.HasValue ? toDate.Value.ToString("dd MMM yyyy") : "All")}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("AUTOMATED ACCOUNTING & PAYROLL")
                                .FontSize(16)
                                .Bold();

                            column.Item()
                                .Text("MANAGEMENT SYSTEM")
                                .FontSize(11);

                            column.Item()
                                .PaddingTop(8)
                                .Text("ATTENDANCE REPORT")
                                .FontSize(20)
                                .Bold();

                            column.Item()
                                .PaddingTop(3)
                                .LineHorizontal(1);
                        });

                    page.Content()
                        .PaddingTop(20)
                        .Column(column =>
                        {
                            column.Item()
                                .Background("#f8fafc")
                                .Border(1)
                                .BorderColor("#e5e7eb")
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text(text =>
                                        {
                                            text.Span("Report Date: ")
                                                .Bold();

                                            text.Span(
                                                DateTime.Now
                                                    .ToString("dd MMM yyyy"));
                                        });

                                    info.Item()
                                        .PaddingTop(4)
                                        .Text(text =>
                                        {
                                            text.Span("Date Range: ")
                                                .Bold();

                                            text.Span(dateRange);
                                        });

                                    info.Item()
                                        .PaddingTop(4)
                                        .Text(text =>
                                        {
                                            text.Span("Employee: ")
                                                .Bold();

                                            text.Span(employeeName);
                                        });

                                    info.Item()
                                        .PaddingTop(4)
                                        .Text(text =>
                                        {
                                            text.Span("Status: ")
                                                .Bold();

                                            text.Span(
                                                string.IsNullOrWhiteSpace(status)
                                                    ? "All Statuses"
                                                    : status);
                                        });
                                });


                            // SUMMARY

                            column.Item()
                                .PaddingTop(15)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    AddSummaryCell(
                                        table,
                                        "TOTAL",
                                        totalRecords.ToString());

                                    AddSummaryCell(
                                        table,
                                        "PRESENT",
                                        presentCount.ToString());

                                    AddSummaryCell(
                                        table,
                                        "ABSENT",
                                        absentCount.ToString());

                                    AddSummaryCell(
                                        table,
                                        "LATE",
                                        lateCount.ToString());

                                    AddSummaryCell(
                                        table,
                                        "LEAVE",
                                        leaveCount.ToString());

                                    AddSummaryCell(
                                        table,
                                        "HALF DAY",
                                        halfDayCount.ToString());
                                });


                            // ATTENDANCE TABLE

                            column.Item()
                                .PaddingTop(20)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.2f);
                                        columns.RelativeColumn(1.3f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1.2f);
                                    });

                                    table.Header(header =>
                                    {
                                        AddHeaderCell(header, "Employee");
                                        AddHeaderCell(header, "Date");
                                        AddHeaderCell(header, "Check In");
                                        AddHeaderCell(header, "Check Out");
                                        AddHeaderCell(header, "Hours");
                                        AddHeaderCell(header, "Late");
                                        AddHeaderCell(header, "Status");
                                    });

                                    foreach (var attendance in attendanceList)
                                    {
                                        AddBodyCell(
                                            table,
                                            $"{attendance.Employee?.FirstName} {attendance.Employee?.LastName}");

                                        AddBodyCell(
                                            table,
                                            attendance.AttendanceDate
                                                .ToString("dd MMM yyyy"));

                                        AddBodyCell(
                                            table,
                                            attendance.CheckInTime?
                                                .ToString(@"hh\:mm") ?? "-");

                                        AddBodyCell(
                                            table,
                                            attendance.CheckOutTime?
                                                .ToString(@"hh\:mm") ?? "-");

                                        AddBodyCell(
                                            table,
                                            attendance.WorkingHours?
                                                .ToString("0.00") ?? "-");

                                        AddBodyCell(
                                            table,
                                            $"{attendance.LateMinutes} min");

                                        AddBodyCell(
                                            table,
                                            attendance.Status);
                                    }
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(
                            $"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt}");
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"Attendance-Report-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }


        // =========================================================
        // PAYROLL REPORT
        // =========================================================

        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> Payroll(
            DateTime? fromMonth,
            DateTime? toMonth,
            int? employeeId,
            string? status)
        {
            var payrolls = _context.Payrolls
                .Include(p => p.Employee)
                .AsQueryable();

            if (fromMonth.HasValue)
            {
                var startMonth = new DateTime(
                    fromMonth.Value.Year,
                    fromMonth.Value.Month,
                    1);

                payrolls = payrolls.Where(p =>
                    p.PayrollMonth >= startMonth);
            }

            if (toMonth.HasValue)
            {
                var endMonth = new DateTime(
                    toMonth.Value.Year,
                    toMonth.Value.Month,
                    1);

                var nextMonth = endMonth.AddMonths(1);

                payrolls = payrolls.Where(p =>
                    p.PayrollMonth < nextMonth);
            }

            if (employeeId.HasValue)
            {
                payrolls = payrolls.Where(p =>
                    p.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                payrolls = payrolls.Where(p =>
                    p.Status == status);
            }

            var payrollList = await payrolls
                .OrderByDescending(p => p.PayrollMonth)
                .ThenBy(p => p.Employee!.FirstName)
                .ThenBy(p => p.Employee!.LastName)
                .ToListAsync();

            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewBag.FromMonth = fromMonth;
            ViewBag.ToMonth = toMonth;
            ViewBag.EmployeeId = employeeId;
            ViewBag.Status = status;

            ViewBag.TotalPayroll = payrollList.Count;
            ViewBag.DraftCount = payrollList.Count(p => p.Status == "Draft");
            ViewBag.ProcessedCount = payrollList.Count(p => p.Status == "Processed");
            ViewBag.PaidCount = payrollList.Count(p => p.Status == "Paid");

            ViewBag.TotalBasicSalary = payrollList.Sum(p => p.BasicSalary);
            ViewBag.TotalAllowances = payrollList.Sum(p => p.Allowances);
            ViewBag.TotalOvertime = payrollList.Sum(p => p.Overtime);
            ViewBag.TotalBonus = payrollList.Sum(p => p.Bonus);
            ViewBag.TotalDeductions = payrollList.Sum(p => p.Deductions);
            ViewBag.TotalLoanDeduction = payrollList.Sum(p => p.LoanDeduction);
            ViewBag.TotalAdvanceSalary = payrollList.Sum(p => p.AdvanceSalary);
            ViewBag.TotalNetSalary = payrollList.Sum(p => p.NetSalary);

            return View(payrollList);
        }


        // =========================================================
        // PAYROLL PDF
        // =========================================================

        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> PayrollPdf(
            DateTime? fromMonth,
            DateTime? toMonth,
            int? employeeId,
            string? status)
        {
            var payrolls = _context.Payrolls
                .Include(p => p.Employee)
                .AsQueryable();

            if (fromMonth.HasValue)
            {
                var startMonth = new DateTime(
                    fromMonth.Value.Year,
                    fromMonth.Value.Month,
                    1);

                payrolls = payrolls.Where(p =>
                    p.PayrollMonth >= startMonth);
            }

            if (toMonth.HasValue)
            {
                var endMonth = new DateTime(
                    toMonth.Value.Year,
                    toMonth.Value.Month,
                    1);

                var nextMonth = endMonth.AddMonths(1);

                payrolls = payrolls.Where(p =>
                    p.PayrollMonth < nextMonth);
            }

            if (employeeId.HasValue)
            {
                payrolls = payrolls.Where(p =>
                    p.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                payrolls = payrolls.Where(p =>
                    p.Status == status);
            }

            var payrollList = await payrolls
                .OrderByDescending(p => p.PayrollMonth)
                .ThenBy(p => p.Employee!.FirstName)
                .ThenBy(p => p.Employee!.LastName)
                .ToListAsync();

            var totalPayroll = payrollList.Count;
            var draftCount = payrollList.Count(p => p.Status == "Draft");
            var processedCount = payrollList.Count(p => p.Status == "Processed");
            var paidCount = payrollList.Count(p => p.Status == "Paid");

            var totalBasicSalary = payrollList.Sum(p => p.BasicSalary);
            var totalAllowances = payrollList.Sum(p => p.Allowances);
            var totalOvertime = payrollList.Sum(p => p.Overtime);
            var totalBonus = payrollList.Sum(p => p.Bonus);
            var totalDeductions = payrollList.Sum(p => p.Deductions);
            var totalLoanDeduction = payrollList.Sum(p => p.LoanDeduction);
            var totalAdvanceSalary = payrollList.Sum(p => p.AdvanceSalary);
            var totalNetSalary = payrollList.Sum(p => p.NetSalary);

            var employeeName = "All Employees";

            if (employeeId.HasValue)
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.EmployeeId == employeeId.Value);

                if (employee != null)
                {
                    employeeName =
                        $"{employee.FirstName} {employee.LastName}";
                }
            }

            var monthRange =
                $"{(fromMonth.HasValue ? fromMonth.Value.ToString("MMM yyyy") : "All")} - " +
                $"{(toMonth.HasValue ? toMonth.Value.ToString("MMM yyyy") : "All")}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("AUTOMATED ACCOUNTING & PAYROLL")
                                .FontSize(16)
                                .Bold();

                            column.Item()
                                .Text("MANAGEMENT SYSTEM")
                                .FontSize(11);

                            column.Item()
                                .PaddingTop(6)
                                .Text("PAYROLL REPORT")
                                .FontSize(20)
                                .Bold();

                            column.Item()
                                .PaddingTop(3)
                                .LineHorizontal(1);
                        });

                    page.Content()
                        .PaddingTop(15)
                        .Column(column =>
                        {
                            column.Item()
                                .Background("#f8fafc")
                                .Border(1)
                                .BorderColor("#e5e7eb")
                                .Padding(8)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text(text =>
                                        {
                                            text.Span("Report Date: ").Bold();
                                            text.Span(DateTime.Now.ToString("dd MMM yyyy"));
                                        });

                                    info.Item()
                                        .PaddingTop(3)
                                        .Text(text =>
                                        {
                                            text.Span("Month Range: ").Bold();
                                            text.Span(monthRange);
                                        });

                                    info.Item()
                                        .PaddingTop(3)
                                        .Text(text =>
                                        {
                                            text.Span("Employee: ").Bold();
                                            text.Span(employeeName);
                                        });

                                    info.Item()
                                        .PaddingTop(3)
                                        .Text(text =>
                                        {
                                            text.Span("Status: ").Bold();
                                            text.Span(
                                                string.IsNullOrWhiteSpace(status)
                                                    ? "All Statuses"
                                                    : status);
                                        });
                                });

                            column.Item()
                                .PaddingTop(12)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    AddSummaryCell(table, "TOTAL PAYROLL", totalPayroll.ToString());
                                    AddSummaryCell(table, "DRAFT", draftCount.ToString());
                                    AddSummaryCell(table, "PROCESSED", processedCount.ToString());
                                    AddSummaryCell(table, "PAID", paidCount.ToString());
                                    AddSummaryCell(table, "BASIC SALARY", totalBasicSalary.ToString("N2"));
                                    AddSummaryCell(table, "ALLOWANCES", totalAllowances.ToString("N2"));
                                    AddSummaryCell(table, "OVERTIME", totalOvertime.ToString("N2"));
                                    AddSummaryCell(table, "BONUS", totalBonus.ToString("N2"));
                                    AddSummaryCell(table, "DEDUCTIONS", totalDeductions.ToString("N2"));
                                    AddSummaryCell(table, "LOAN DEDUCTION", totalLoanDeduction.ToString("N2"));
                                    AddSummaryCell(table, "ADVANCE SALARY", totalAdvanceSalary.ToString("N2"));
                                    AddSummaryCell(table, "NET SALARY", totalNetSalary.ToString("N2"));
                                });

                            column.Item()
                                .PaddingTop(15)
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2.2f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.1f);
                                        columns.RelativeColumn(1.0f);
                                        columns.RelativeColumn(1.2f);
                                    });

                                    table.Header(header =>
                                    {
                                        AddHeaderCell(header, "Employee");
                                        AddHeaderCell(header, "Month");
                                        AddHeaderCell(header, "Basic");
                                        AddHeaderCell(header, "Allowances");
                                        AddHeaderCell(header, "Overtime");
                                        AddHeaderCell(header, "Bonus");
                                        AddHeaderCell(header, "Deductions");
                                        AddHeaderCell(header, "Loan");
                                        AddHeaderCell(header, "Advance");
                                        AddHeaderCell(header, "Net Salary");
                                        AddHeaderCell(header, "Status");
                                        AddHeaderCell(header, "Payment Date");
                                    });

                                    foreach (var payroll in payrollList)
                                    {
                                        AddBodyCell(
                                            table,
                                            $"{payroll.Employee?.FirstName} {payroll.Employee?.LastName}");

                                        AddBodyCell(
                                            table,
                                            payroll.PayrollMonth.ToString("MMM yyyy"));

                                        AddBodyCell(table, payroll.BasicSalary.ToString("N2"));
                                        AddBodyCell(table, payroll.Allowances.ToString("N2"));
                                        AddBodyCell(table, payroll.Overtime.ToString("N2"));
                                        AddBodyCell(table, payroll.Bonus.ToString("N2"));
                                        AddBodyCell(table, payroll.Deductions.ToString("N2"));
                                        AddBodyCell(table, payroll.LoanDeduction.ToString("N2"));
                                        AddBodyCell(table, payroll.AdvanceSalary.ToString("N2"));
                                        AddBodyCell(table, payroll.NetSalary.ToString("N2"));
                                        AddBodyCell(table, payroll.Status);

                                        AddBodyCell(
                                            table,
                                            payroll.PaymentDate.HasValue
                                                ? payroll.PaymentDate.Value.ToString("dd MMM yyyy")
                                                : "-");
                                    }
                                });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(
                            $"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt}");
                });
            });

            var pdfBytes = document.GeneratePdf();

            return File(
                pdfBytes,
                "application/pdf",
                $"Payroll-Report-{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }


        // =========================================================
        // ATTENDANCE PDF HELPER METHODS
        // =========================================================

        private static void AddHeaderCell(
            TableCellDescriptor header,
            string text)
        {
            header.Cell()
                .Background("#f3f4f6")
                .Border(1)
                .BorderColor("#d1d5db")
                .Padding(6)
                .Text(text)
                .FontSize(8)
                .Bold();
        }


        private static void AddBodyCell(
            TableDescriptor table,
            string text)
        {
            table.Cell()
                .BorderBottom(1)
                .BorderColor("#e5e7eb")
                .Padding(6)
                .Text(text)
                .FontSize(8);
        }


        private static void AddSummaryCell(
            TableDescriptor table,
            string label,
            string value)
        {
            table.Cell()
                .Background("#f8fafc")
                .Border(1)
                .BorderColor("#e5e7eb")
                .Padding(8)
                .Column(column =>
                {
                    column.Item()
                        .Text(label)
                        .FontSize(7)
                        .Bold();

                    column.Item()
                        .PaddingTop(3)
                        .Text(value)
                        .FontSize(14)
                        .Bold();
                });
        }
    }
}