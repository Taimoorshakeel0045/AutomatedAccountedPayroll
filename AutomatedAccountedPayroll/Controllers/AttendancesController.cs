using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AttendancesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly TimeSpan OfficeStartTime =
            new TimeSpan(8, 0, 0);

        public AttendancesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Attendances
        public async Task<IActionResult> Index(
            DateTime? attendanceDate,
            int? employeeId,
            string? status)
        {
            var attendances = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (attendanceDate.HasValue)
            {
                var selectedDate = attendanceDate.Value.Date;

                attendances = attendances.Where(a =>
                    a.AttendanceDate.Date == selectedDate);
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

            ViewData["AttendanceDate"] = attendanceDate;
            ViewData["EmployeeId"] = employeeId;
            ViewData["Status"] = status;

            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

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

        // GET: Attendances/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a =>
                    a.AttendanceId == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // GET: Attendances/Create
        public async Task<IActionResult> Create()
        {
            await LoadEmployees();

            return View();
        }

        // POST: Attendances/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("AttendanceId,EmployeeId,AttendanceDate,Status,CheckInTime,CheckOutTime,Remarks,CreatedAt")]
            Attendance attendance,
            string attendanceType)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e =>
                    e.EmployeeId == attendance.EmployeeId &&
                    e.IsActive);

            if (!employeeExists)
            {
                ModelState.AddModelError(
                    "EmployeeId",
                    "Please select a valid active employee.");
            }

            var duplicateAttendance = await _context.Attendances
                .AnyAsync(a =>
                    a.EmployeeId == attendance.EmployeeId &&
                    a.AttendanceDate.Date ==
                    attendance.AttendanceDate.Date);

            if (duplicateAttendance)
            {
                ModelState.AddModelError(
                    "AttendanceDate",
                    "Attendance already exists for this employee on the selected date.");
            }

            ValidateAttendance(attendance, attendanceType);

            if (ModelState.IsValid)
            {
                CalculateAttendanceValues(
                    attendance,
                    attendanceType);

                attendance.CreatedAt = DateTime.UtcNow;

                _context.Attendances.Add(attendance);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadEmployees(attendance.EmployeeId);

            ViewData["AttendanceType"] = attendanceType;

            return View(attendance);
        }

        // GET: Attendances/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.AttendanceId == id);

            if (attendance == null)
            {
                return NotFound();
            }

            await LoadEmployees(attendance.EmployeeId);

            ViewData["AttendanceType"] = GetAttendanceType(
                attendance.Status);

            return View(attendance);
        }

        // POST: Attendances/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("AttendanceId,EmployeeId,AttendanceDate,Status,CheckInTime,CheckOutTime,Remarks,CreatedAt")]
            Attendance attendance,
            string attendanceType)
        {
            if (id != attendance.AttendanceId)
            {
                return NotFound();
            }

            var employeeExists = await _context.Employees
                .AnyAsync(e =>
                    e.EmployeeId == attendance.EmployeeId &&
                    e.IsActive);

            if (!employeeExists)
            {
                ModelState.AddModelError(
                    "EmployeeId",
                    "Please select a valid active employee.");
            }

            var duplicateAttendance = await _context.Attendances
                .AnyAsync(a =>
                    a.AttendanceId != attendance.AttendanceId &&
                    a.EmployeeId == attendance.EmployeeId &&
                    a.AttendanceDate.Date ==
                    attendance.AttendanceDate.Date);

            if (duplicateAttendance)
            {
                ModelState.AddModelError(
                    "AttendanceDate",
                    "Attendance already exists for this employee on the selected date.");
            }

            ValidateAttendance(attendance, attendanceType);

            if (ModelState.IsValid)
            {
                CalculateAttendanceValues(
                    attendance,
                    attendanceType);

                try
                {
                    var existingAttendance =
                        await _context.Attendances
                            .FirstOrDefaultAsync(a =>
                                a.AttendanceId ==
                                attendance.AttendanceId);

                    if (existingAttendance == null)
                    {
                        return NotFound();
                    }

                    existingAttendance.EmployeeId =
                        attendance.EmployeeId;

                    existingAttendance.AttendanceDate =
                        attendance.AttendanceDate;

                    existingAttendance.Status =
                        attendance.Status;

                    existingAttendance.CheckInTime =
                        attendance.CheckInTime;

                    existingAttendance.CheckOutTime =
                        attendance.CheckOutTime;

                    existingAttendance.WorkingHours =
                        attendance.WorkingHours;

                    existingAttendance.LateMinutes =
                        attendance.LateMinutes;

                    existingAttendance.Remarks =
                        attendance.Remarks;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceExists(
                        attendance.AttendanceId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadEmployees(attendance.EmployeeId);

            ViewData["AttendanceType"] = attendanceType;

            return View(attendance);
        }

        // GET: Attendances/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a =>
                    a.AttendanceId == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // POST: Attendances/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances
                .FindAsync(id);

            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);

                await _context.SaveChangesAsync();
            }

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

        private void ValidateAttendance(
            Attendance attendance,
            string attendanceType)
        {
            var validTypes = new[]
            {
                "Normal",
                "Absent",
                "Leave",
                "Half Day"
            };

            if (!validTypes.Contains(attendanceType))
            {
                ModelState.AddModelError(
                    "Status",
                    "Please select a valid attendance type.");

                return;
            }

            if (attendanceType == "Absent" ||
                attendanceType == "Leave")
            {
                attendance.CheckInTime = null;
                attendance.CheckOutTime = null;
                attendance.WorkingHours = null;
                attendance.LateMinutes = 0;

                return;
            }

            if (!attendance.CheckInTime.HasValue)
            {
                ModelState.AddModelError(
                    "CheckInTime",
                    "Check-in time is required.");
            }

            if (!attendance.CheckOutTime.HasValue)
            {
                ModelState.AddModelError(
                    "CheckOutTime",
                    "Check-out time is required.");
            }

            if (attendance.CheckInTime.HasValue &&
                attendance.CheckOutTime.HasValue &&
                attendance.CheckOutTime.Value <
                attendance.CheckInTime.Value)
            {
                ModelState.AddModelError(
                    "CheckOutTime",
                    "Check-out time cannot be earlier than check-in time.");
            }
        }

        private void CalculateAttendanceValues(
            Attendance attendance,
            string attendanceType)
        {
            attendance.WorkingHours = null;
            attendance.LateMinutes = 0;

            if (attendanceType == "Absent")
            {
                attendance.Status = "Absent";
                attendance.CheckInTime = null;
                attendance.CheckOutTime = null;
                return;
            }

            if (attendanceType == "Leave")
            {
                attendance.Status = "Leave";
                attendance.CheckInTime = null;
                attendance.CheckOutTime = null;
                return;
            }

            if (attendanceType == "Half Day")
            {
                attendance.Status = "Half Day";
            }
            else if (attendance.CheckInTime.HasValue)
            {
                if (attendance.CheckInTime.Value >
                    OfficeStartTime)
                {
                    attendance.Status = "Late";
                }
                else
                {
                    attendance.Status = "Present";
                }
            }

            if (attendance.CheckInTime.HasValue &&
                attendance.CheckOutTime.HasValue)
            {
                var duration =
                    attendance.CheckOutTime.Value -
                    attendance.CheckInTime.Value;

                attendance.WorkingHours =
                    Math.Round(
                        (decimal)duration.TotalHours,
                        2);
            }

            if (attendance.CheckInTime.HasValue &&
                attendance.CheckInTime.Value >
                OfficeStartTime &&
                attendance.Status == "Late")
            {
                var lateDifference =
                    attendance.CheckInTime.Value -
                    OfficeStartTime;

                attendance.LateMinutes =
                    (int)Math.Floor(
                        lateDifference.TotalMinutes);
            }
        }

        private string GetAttendanceType(string status)
        {
            return status switch
            {
                "Absent" => "Absent",
                "Leave" => "Leave",
                "Half Day" => "Half Day",
                _ => "Normal"
            };
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances
                .Any(e => e.AttendanceId == id);
        }
    }
}