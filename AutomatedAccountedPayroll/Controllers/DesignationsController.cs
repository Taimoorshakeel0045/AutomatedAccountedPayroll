using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin,HR,Accountant")]
    public class DesignationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Designations
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.TotalDesignations = await _context.Designations
                .CountAsync();

            ViewBag.ActiveDesignations = await _context.Designations
                .CountAsync(d => d.IsActive);

            ViewBag.InactiveDesignations = await _context.Designations
                .CountAsync(d => !d.IsActive);

            ViewBag.DepartmentsWithDesignations = await _context.Designations
                .Where(d => d.DepartmentId.HasValue)
                .Select(d => d.DepartmentId)
                .Distinct()
                .CountAsync();

            return View(departments);
        }


        // GET: Designations/DepartmentDesignations/5
        public async Task<IActionResult> DepartmentDesignations(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                return NotFound();
            }

            var designations = await _context.Designations
                .Where(d => d.DepartmentId == id)
                .OrderBy(d => d.DesignationName)
                .ToListAsync();

            ViewBag.TotalDesignations = designations.Count;
            ViewBag.ActiveDesignations = designations.Count(d => d.IsActive);
            ViewBag.InactiveDesignations = designations.Count(d => !d.IsActive);
            ViewBag.Department = department;

            return View(
                "~/Views/Designations/DepartmentDesignations.cshtml",
                designations);
        }


        // GET: Designations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designations
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.DesignationId == id);

            if (designation == null)
            {
                return NotFound();
            }

            return View(designation);
        }

        // GET: Designations/Create
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(int? departmentId)
        {
            if (departmentId == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

            if (department == null)
            {
                return NotFound();
            }

            var designation = new Designation
            {
                DepartmentId = department.DepartmentId
            };

            ViewBag.SelectedDepartmentName = department.DepartmentName;

            return View(designation);
        }


        // POST: Designations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(
            [Bind("DesignationId,DesignationName,Description,DepartmentId,IsActive")]
            Designation designation)
        {
            if (designation.DepartmentId == null)
            {
                ModelState.AddModelError(
                    "DepartmentId",
                    "Please select a department.");
            }
            else
            {
                var duplicateExists = await _context.Designations
                    .AnyAsync(d =>
                        d.DepartmentId == designation.DepartmentId &&
                        d.DesignationName.ToLower() ==
                        designation.DesignationName.ToLower());

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        "DesignationName",
                        "This designation already exists in this department.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(designation);
                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(DepartmentDesignations),
                    new { id = designation.DepartmentId });
            }

            if (designation.DepartmentId != null)
            {
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d =>
                        d.DepartmentId == designation.DepartmentId);

                ViewBag.SelectedDepartmentName =
                    department?.DepartmentName;
            }

            return View(designation);
        }


        // GET: Designations/Edit/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designations.FindAsync(id);

            if (designation == null)
            {
                return NotFound();
            }

            await LoadDepartments(designation.DepartmentId);

            return View(designation);
        }

        // POST: Designations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("DesignationId,DesignationName,Description,DepartmentId,IsActive")]
            Designation designation)
        {
            if (id != designation.DesignationId)
            {
                return NotFound();
            }

            if (designation.DepartmentId.HasValue)
            {
                var departmentExists = await _context.Departments
                    .AnyAsync(d =>
                        d.DepartmentId == designation.DepartmentId.Value &&
                        d.IsActive);

                if (!departmentExists)
                {
                    ModelState.AddModelError(
                        "DepartmentId",
                        "Please select a valid active department.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(designation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DesignationExists(designation.DesignationId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadDepartments(designation.DepartmentId);

            return View(designation);
        }

        // GET: Designations/Delete/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var designation = await _context.Designations
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.DesignationId == id);

            if (designation == null)
            {
                return NotFound();
            }

            return View(designation);
        }

        // POST: Designations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designation = await _context.Designations.FindAsync(id);

            if (designation != null)
            {
                _context.Designations.Remove(designation);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Load active departments for dropdown
        private async Task LoadDepartments(int? selectedDepartmentId = null)
        {
            ViewData["DepartmentId"] = new SelectList(
                await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync(),
                "DepartmentId",
                "DepartmentName",
                selectedDepartmentId);
        }

        private bool DesignationExists(int id)
        {
            return _context.Designations.Any(e => e.DesignationId == id);
        }
    }
}