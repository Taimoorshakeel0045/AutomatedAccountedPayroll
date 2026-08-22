using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeavesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeavesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Leaves
        public async Task<IActionResult> Index(
            int? employeeId,
            string? leaveType,
            string? status)
        {
            var leaves = _context.Leaves
                .Include(l => l.Employee)
                .AsQueryable();

            if (employeeId.HasValue)
            {
                leaves = leaves.Where(l =>
                    l.EmployeeId == employeeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(leaveType))
            {
                leaves = leaves.Where(l =>
                    l.LeaveType == leaveType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                leaves = leaves.Where(l =>
                    l.Status == status);
            }

            var leaveList = await leaves
                .OrderByDescending(l => l.AppliedDate)
                .ThenByDescending(l => l.StartDate)
                .ThenBy(l => l.Employee!.FirstName)
                .ToListAsync();

            ViewData["EmployeeId"] = employeeId;
            ViewData["LeaveType"] = leaveType;
            ViewData["Status"] = status;

            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewBag.TotalLeaves = leaveList.Count;
            ViewBag.PendingCount =
                leaveList.Count(l => l.Status == "Pending");

            ViewBag.ApprovedCount =
                leaveList.Count(l => l.Status == "Approved");

            ViewBag.RejectedCount =
                leaveList.Count(l => l.Status == "Rejected");

            ViewBag.TotalLeaveDays =
                leaveList.Sum(l => l.TotalDays);

            return View(leaveList);
        }

        // GET: Leaves/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l =>
                    l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            return View(leave);
        }

        // GET: Leaves/Create
        public async Task<IActionResult> Create()
        {
            await LoadEmployees();

            return View();
        }

        // POST: Leaves/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LeaveId,EmployeeId,LeaveType,StartDate,EndDate,Reason,Remarks")]
            Leave leave)
        {
            ValidateLeaveDates(leave);

            if (ModelState.IsValid)
            {
                leave.TotalDays =
                    CalculateTotalDays(
                        leave.StartDate,
                        leave.EndDate);

                leave.Status = "Pending";
                leave.AppliedDate = DateTime.UtcNow;
                leave.ApprovedBy = null;
                leave.ApprovedDate = null;

                var overlappingLeave =
                    await HasOverlappingApprovedOrPendingLeave(leave);

                if (overlappingLeave)
                {
                    ModelState.AddModelError(
                        "StartDate",
                        "This employee already has a pending or approved leave that overlaps the selected dates.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Leaves.Add(leave);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadEmployees(leave.EmployeeId);

            return View(leave);
        }

        // GET: Leaves/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l =>
                    l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leave requests can be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = leave.LeaveId });
            }

            await LoadEmployees(leave.EmployeeId);

            return View(leave);
        }

        // POST: Leaves/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("LeaveId,EmployeeId,LeaveType,StartDate,EndDate,Reason,Remarks")]
            Leave leave)
        {
            if (id != leave.LeaveId)
            {
                return NotFound();
            }

            var existingLeave =
                await _context.Leaves
                    .FirstOrDefaultAsync(l =>
                        l.LeaveId == id);

            if (existingLeave == null)
            {
                return NotFound();
            }

            if (existingLeave.Status != "Pending")
            {
                ModelState.AddModelError(
                    "",
                    "Only pending leave requests can be edited.");
            }

            ValidateLeaveDates(leave);

            if (ModelState.IsValid)
            {
                var overlappingLeave =
                    await HasOverlappingApprovedOrPendingLeave(
                        leave,
                        leave.LeaveId);

                if (overlappingLeave)
                {
                    ModelState.AddModelError(
                        "StartDate",
                        "This employee already has a pending or approved leave that overlaps the selected dates.");
                }
            }

            if (ModelState.IsValid)
            {
                existingLeave.EmployeeId =
                    leave.EmployeeId;

                existingLeave.LeaveType =
                    leave.LeaveType;

                existingLeave.StartDate =
                    leave.StartDate;

                existingLeave.EndDate =
                    leave.EndDate;

                existingLeave.TotalDays =
                    CalculateTotalDays(
                        leave.StartDate,
                        leave.EndDate);

                existingLeave.Reason =
                    leave.Reason;

                existingLeave.Remarks =
                    leave.Remarks;

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Details),
                    new { id = leave.LeaveId });
            }

            await LoadEmployees(leave.EmployeeId);

            return View(leave);
        }

        // POST: Leaves/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l =>
                    l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leave requests can be approved.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Check for another approved leave overlapping this request.
            var overlappingLeave =
                await HasOverlappingApprovedOrPendingLeave(
                    leave,
                    leave.LeaveId,
                    onlyApproved: true);

            if (overlappingLeave)
            {
                TempData["Error"] =
                    "This leave cannot be approved because it overlaps another approved leave for the employee.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Check whether attendance already exists for any leave date.
            var existingAttendanceDates = await _context.Attendances
                .Where(a =>
                    a.EmployeeId == leave.EmployeeId &&
                    a.AttendanceDate.Date >= leave.StartDate.Date &&
                    a.AttendanceDate.Date <= leave.EndDate.Date)
                .Select(a => a.AttendanceDate.Date)
                .ToListAsync();

            if (existingAttendanceDates.Any())
            {
                TempData["Error"] =
                    "This leave cannot be approved because attendance already exists for one or more requested leave dates.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Approve the leave.
            leave.Status = "Approved";

            leave.ApprovedBy =
                User.Identity?.Name ?? "Admin";

            leave.ApprovedDate =
                DateTime.UtcNow;

            // Create Attendance records for each leave date.
            var attendanceRecords = new List<Attendance>();

            for (var date = leave.StartDate.Date;
                 date <= leave.EndDate.Date;
                 date = date.AddDays(1))
            {
                attendanceRecords.Add(
                    new Attendance
                    {
                        EmployeeId = leave.EmployeeId,
                        AttendanceDate = date,
                        Status = "Leave",
                        CheckInTime = null,
                        CheckOutTime = null,
                        WorkingHours = null,
                        LateMinutes = 0,
                        Remarks = $"Approved {leave.LeaveType} leave.",
                        CreatedAt = DateTime.UtcNow
                    });
            }

            _context.Attendances.AddRange(attendanceRecords);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // POST: Leaves/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? remarks)
        {
            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l =>
                    l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leave requests can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            leave.Status = "Rejected";
            leave.ApprovedBy =
                User.Identity?.Name ?? "Admin";

            leave.ApprovedDate =
                DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                leave.Remarks = remarks;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // GET: Leaves/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l =>
                    l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            return View(leave);
        }

        // POST: Leaves/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.LeaveId == id);

            if (leave == null)
            {
                return NotFound();
            }

            if (leave.Status == "Approved")
            {
                TempData["Error"] =
                    "Approved leave requests cannot be deleted because they may have generated Attendance records.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = leave.LeaveId });
            }

            _context.Leaves.Remove(leave);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        private async Task LoadEmployees(
            int? selectedEmployeeId = null)
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            ViewData["EmployeeId"] = new SelectList(
                employees,
                "EmployeeId",
                "EmployeeCode",
                selectedEmployeeId);
        }

        private void ValidateLeaveDates(Leave leave)
        {
            if (leave.EndDate.Date < leave.StartDate.Date)
            {
                ModelState.AddModelError(
                    "EndDate",
                    "End date cannot be earlier than start date.");
            }

            var validTypes = new[]
            {
                "Casual",
                "Sick",
                "Annual",
                "Emergency",
                "Unpaid"
            };

            if (!validTypes.Contains(leave.LeaveType))
            {
                ModelState.AddModelError(
                    "LeaveType",
                    "Please select a valid leave type.");
            }

            leave.StartDate = leave.StartDate.Date;
            leave.EndDate = leave.EndDate.Date;
        }

        private decimal CalculateTotalDays(
            DateTime startDate,
            DateTime endDate)
        {
            return (decimal)
                ((endDate.Date - startDate.Date).Days + 1);
        }

        private async Task<bool> HasOverlappingApprovedOrPendingLeave(
            Leave leave,
            int? excludeLeaveId = null,
            bool onlyApproved = false)
        {
            var query = _context.Leaves
                .Where(l =>
                    l.EmployeeId == leave.EmployeeId &&
                    l.LeaveId != excludeLeaveId &&
                    l.StartDate <= leave.EndDate &&
                    l.EndDate >= leave.StartDate);

            if (onlyApproved)
            {
                query = query.Where(l =>
                    l.Status == "Approved");
            }
            else
            {
                query = query.Where(l =>
                    l.Status == "Pending" ||
                    l.Status == "Approved");
            }

            return await query.AnyAsync();
        }
    }
}