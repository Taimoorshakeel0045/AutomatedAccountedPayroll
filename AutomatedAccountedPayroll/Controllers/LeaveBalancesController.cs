using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    public class LeaveBalancesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalancesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LeaveBalances
        
        public async Task<IActionResult> Index()
        {
            var leaveBalances = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .OrderByDescending(lb => lb.Year)
                .ThenBy(lb => lb.Employee!.EmployeeCode)
                .ThenBy(lb => lb.LeaveType)
                .ToListAsync();

            return View(leaveBalances);
        }


        // GET: LeaveBalances/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveBalance = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .FirstOrDefaultAsync(
                    lb => lb.LeaveBalanceId == id);

            if (leaveBalance == null)
            {
                return NotFound();
            }

            return View(leaveBalance);
        }

        // GET: LeaveBalances/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = await _context.Employees
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            return View();
        }

        // POST: LeaveBalances/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EmployeeId,Year,LeaveType,AllocatedDays")]
            LeaveBalance leaveBalance)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Employees = await _context.Employees
                    .OrderBy(e => e.EmployeeCode)
                    .ToListAsync();

                return View(leaveBalance);
            }

            var duplicateExists =
                await _context.LeaveBalances.AnyAsync(lb =>
                    lb.EmployeeId == leaveBalance.EmployeeId &&
                    lb.Year == leaveBalance.Year &&
                    lb.LeaveType == leaveBalance.LeaveType);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "",
                    "A leave balance already exists for this employee, year, and leave type.");

                ViewBag.Employees = await _context.Employees
                    .OrderBy(e => e.EmployeeCode)
                    .ToListAsync();

                return View(leaveBalance);
            }

            leaveBalance.UsedDays = 0;

            _context.LeaveBalances.Add(leaveBalance);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: LeaveBalances/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveBalance = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .FirstOrDefaultAsync(
                    lb => lb.LeaveBalanceId == id);

            if (leaveBalance == null)
            {
                return NotFound();
            }

            ViewBag.Employees = await _context.Employees
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            return View(leaveBalance);
        }

        // POST: LeaveBalances/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("LeaveBalanceId,EmployeeId,Year,LeaveType,AllocatedDays")]
            LeaveBalance leaveBalance)
        {
            if (id != leaveBalance.LeaveBalanceId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Employees = await _context.Employees
                    .OrderBy(e => e.EmployeeCode)
                    .ToListAsync();

                return View(leaveBalance);
            }

            var existingBalance =
                await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb =>
                        lb.LeaveBalanceId == id);

            if (existingBalance == null)
            {
                return NotFound();
            }

            var duplicateExists =
                await _context.LeaveBalances.AnyAsync(lb =>
                    lb.LeaveBalanceId != id &&
                    lb.EmployeeId == leaveBalance.EmployeeId &&
                    lb.Year == leaveBalance.Year &&
                    lb.LeaveType == leaveBalance.LeaveType);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "",
                    "Another leave balance already exists for this employee, year, and leave type.");

                ViewBag.Employees = await _context.Employees
                    .OrderBy(e => e.EmployeeCode)
                    .ToListAsync();

                return View(leaveBalance);
            }

            existingBalance.EmployeeId =
                leaveBalance.EmployeeId;

            existingBalance.Year =
                leaveBalance.Year;

            existingBalance.LeaveType =
                leaveBalance.LeaveType;

            existingBalance.AllocatedDays =
                leaveBalance.AllocatedDays;

            // UsedDays is controlled by the system.
            // Do not overwrite it during editing.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeaveBalanceExists(id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: LeaveBalances/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveBalance = await _context.LeaveBalances
                .Include(lb => lb.Employee)
                .FirstOrDefaultAsync(
                    lb => lb.LeaveBalanceId == id);

            if (leaveBalance == null)
            {
                return NotFound();
            }

            return View(leaveBalance);
        }

        // POST: LeaveBalances/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leaveBalance =
                await _context.LeaveBalances.FindAsync(id);

            if (leaveBalance == null)
            {
                return NotFound();
            }

            _context.LeaveBalances.Remove(leaveBalance);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool LeaveBalanceExists(int id)
        {
            return _context.LeaveBalances
                .Any(e => e.LeaveBalanceId == id);
        }
    }
}