
using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index(
            string? searchString,
            int? departmentId,
            string? status)
        {
            var employees = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                employees = employees.Where(e =>
                    e.EmployeeCode.Contains(searchString) ||
                    e.FirstName.Contains(searchString) ||
                    e.LastName.Contains(searchString) ||
                    (e.CNIC != null && e.CNIC.Contains(searchString)) ||
                    (e.Phone != null && e.Phone.Contains(searchString)));
            }

            if (departmentId.HasValue)
            {
                employees = employees.Where(e =>
                    e.DepartmentId == departmentId.Value);
            }

            if (status == "Active")
            {
                employees = employees.Where(e => e.IsActive);
            }
            else if (status == "Inactive")
            {
                employees = employees.Where(e => !e.IsActive);
            }

            ViewData["SearchString"] = searchString;
            ViewData["DepartmentId"] = departmentId;
            ViewData["Status"] = status;

            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            return View(await employees.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("EmployeeId,EmployeeCode,FirstName,LastName,FatherName,CNIC,Phone,Email,Address,JoiningDate,DepartmentId,DesignationId,EmploymentType,BasicSalary,IsActive")]
            Employee employee)
        {
            // Check whether Employee Code already exists
            var employeeCodeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeCode == employee.EmployeeCode);

            if (employeeCodeExists)
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Employee Code already exists. Please use a different Employee Code.");
            }

            // Make sure the selected designation belongs to the selected department
            if (employee.DepartmentId > 0 &&
    employee.DesignationId > 0)
            {
                var designationBelongsToDepartment =
                    await _context.Designations.AnyAsync(d =>
                        d.DesignationId == employee.DesignationId &&
                        d.DepartmentId == employee.DepartmentId &&
                        d.IsActive);

                if (!designationBelongsToDepartment)
                {
                    ModelState.AddModelError(
                        "DesignationId",
                        "The selected designation does not belong to the selected department.");
                }
            }

            if (ModelState.IsValid)
            {
                employee.CreatedAt = DateTime.UtcNow;

                _context.Employees.Add(employee);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(
                employee.DepartmentId,
                employee.DesignationId);

            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            await LoadDropdowns(
                employee.DepartmentId,
                employee.DesignationId);

            return View(employee);
        }

        // GET: Employees/GetDesignations
        [HttpGet]
        public async Task<IActionResult> GetDesignations(int departmentId)
        {
            var designations = await _context.Designations
                .Where(d =>
                    d.DepartmentId == departmentId &&
                    d.IsActive)
                .OrderBy(d => d.DesignationName)
                .Select(d => new
                {
                    id = d.DesignationId,
                    name = d.DesignationName
                })
                .ToListAsync();

            return Json(designations);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("EmployeeId,EmployeeCode,FirstName,LastName,FatherName,CNIC,Phone,Email,Address,JoiningDate,DepartmentId,DesignationId,EmploymentType,BasicSalary,IsActive,CreatedAt")]
            Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            // Make sure the selected designation belongs to the selected department
            if (employee.DepartmentId > 0 &&
    employee.DesignationId > 0)
            {
                var designationBelongsToDepartment =
                    await _context.Designations.AnyAsync(d =>
                        d.DesignationId == employee.DesignationId &&
                        d.DepartmentId == employee.DepartmentId &&
                        d.IsActive);

                if (!designationBelongsToDepartment)
                {
                    ModelState.AddModelError(
                        "DesignationId",
                        "The selected designation does not belong to the selected department.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(
                employee.DepartmentId,
                employee.DesignationId);

            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee != null)
            {
                _context.Employees.Remove(employee);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // designation auto selected 
        // Load departments and department-specific designations
        private async Task LoadDropdowns(
int? selectedDepartmentId = null,
int? selectedDesignationId = null)
        {
            var departments = await _context.Departments
            .Where(d => d.IsActive)
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();


ViewBag.DepartmentId = new SelectList(
    departments,
    "DepartmentId",
    "DepartmentName",
    selectedDepartmentId
);

            var designations = new List<Designation>();

            if (selectedDepartmentId.HasValue)
            {
                designations = await _context.Designations
                    .Where(d =>
                        d.DepartmentId == selectedDepartmentId.Value &&
                        d.IsActive)
                    .OrderBy(d => d.DesignationName)
                    .ToListAsync();
            }

            ViewBag.DesignationList = designations;

            ViewBag.DesignationId = new SelectList(
                designations,
                "DesignationId",
                "DesignationName",
                selectedDesignationId
            );


}

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }
    }
}
