using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using AutomatedAccountedPayroll.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin,HR,Accountant")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EmployeesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            ViewData["Status"] = status;
            ViewBag.SelectedDepartmentId = departmentId;

            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.TotalEmployees = await _context.Employees.CountAsync();

            ViewBag.ActiveEmployees = await _context.Employees
                .CountAsync(e => e.IsActive);

            ViewBag.InactiveEmployees = await _context.Employees
                .CountAsync(e => !e.IsActive);

            ViewBag.PermanentEmployees = await _context.Employees
                .CountAsync(e => e.EmploymentType == "Permanent");

            ViewBag.OtherEmploymentEmployees = await _context.Employees
                .CountAsync(e => e.EmploymentType != "Permanent");

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
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(
     [Bind("EmployeeId,EmployeeCode,FirstName,LastName,FatherName,CNIC,Phone,Email,Address,JoiningDate,DepartmentId,DesignationId,EmploymentType,BasicSalary,IsActive")]
            Employee employee,
     bool createLogin,
     string? loginEmail,
     string? loginPassword)
        {
            var employeeCodeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeCode == employee.EmployeeCode);

            if (employeeCodeExists)
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Employee Code already exists. Please use a different Employee Code.");
            }

            if (employee.DepartmentId <= 0)
            {
                ModelState.AddModelError(
                    "DepartmentId",
                    "Please select a department.");
            }

            if (employee.DesignationId <= 0)
            {
                ModelState.AddModelError(
                    "DesignationId",
                    "Please select a designation.");
            }
            else if (employee.DepartmentId > 0)
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

            // Validate optional login fields if createLogin = true
            if (createLogin)
            {
                if (string.IsNullOrWhiteSpace(loginEmail))
                {
                    ModelState.AddModelError(
                        "loginEmail",
                        "Login email is required when creating a login account.");
                }
                else
                {
                    var existing = await _userManager.FindByEmailAsync(loginEmail);
                    if (existing != null)
                    {
                        ModelState.AddModelError(
                            "loginEmail",
                            "A user with this email already exists.");
                    }
                }

                if (string.IsNullOrWhiteSpace(loginPassword))
                {
                    ModelState.AddModelError(
                        "loginPassword",
                        "Login password is required when creating a login account.");
                }
            }

            if (ModelState.IsValid)
            {
                employee.CreatedAt = DateTime.UtcNow;

                _context.Employees.Add(employee);

                await _context.SaveChangesAsync();

                // Optional: create linked login account
                if (createLogin)
                {
                    var user = new IdentityUser
                    {
                        UserName = loginEmail,
                        Email = loginEmail,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, loginPassword!);

                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");

                        employee.IdentityUserId = user.Id;
                        await _context.SaveChangesAsync();

                        TempData["Success"] =
                            "Employee and login account created successfully.";
                    }
                    else
                    {
                        TempData["Error"] =
                            "Employee created, but login failed: " +
                            string.Join(" ", result.Errors.Select(e => e.Description));
                    }
                }
                else
                {
                    TempData["Success"] = "Employee created successfully.";
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(
                employee.DepartmentId,
                employee.DesignationId);

            return View(employee);
        }

        // GET: Employees/Edit/5
        [Authorize(Roles = "Admin,HR")]
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
        [Authorize(Roles = "Admin,HR")]
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
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("EmployeeId,EmployeeCode,FirstName,LastName,FatherName,CNIC,Phone,Email,Address,JoiningDate,DepartmentId,DesignationId,EmploymentType,BasicSalary,IsActive,CreatedAt")]
            Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            if (employee.DepartmentId <= 0)
            {
                ModelState.AddModelError(
                    "DepartmentId",
                    "Please select a department.");
            }

            if (employee.DesignationId <= 0)
            {
                ModelState.AddModelError(
                    "DesignationId",
                    "Please select a designation.");
            }
            else if (employee.DepartmentId > 0)
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


        // GET: Employees/ManageAccount/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ManageAccount(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            var model = new EmployeeAccountViewModel
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                EmployeeCode = employee.EmployeeCode,
                CurrentUserId = employee.IdentityUserId
            };

            if (!string.IsNullOrWhiteSpace(employee.IdentityUserId))
            {
                var currentUser =
                    await _userManager.FindByIdAsync(employee.IdentityUserId);

                model.CurrentUserEmail = currentUser?.Email;
            }

            var allUsers = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (!roles.Contains("User"))
                {
                    continue;
                }

                var linkedToEmployee = await _context.Employees
                    .AnyAsync(e =>
                        e.IdentityUserId == user.Id &&
                        e.EmployeeId != employee.EmployeeId);

                if (!linkedToEmployee)
                {
                    model.AvailableUsers.Add(new SelectListItem
                    {
                        Value = user.Id,
                        Text = user.Email,
                        Selected = user.Id == employee.IdentityUserId
                    });
                }
            }

            return View(model);
        }


        // POST: Employees/LinkAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> LinkAccount(
            int employeeId,
            string? selectedUserId)
        {
            if (employeeId <= 0)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            if (!employee.IsActive)
            {
                TempData["Error"] =
                    "An inactive employee cannot be linked to a login account.";

                return RedirectToAction(
                    nameof(ManageAccount),
                    new { id = employeeId });
            }

            if (string.IsNullOrWhiteSpace(selectedUserId))
            {
                TempData["Error"] =
                    "Please select a User account.";

                return RedirectToAction(
                    nameof(ManageAccount),
                    new { id = employeeId });
            }

            var user = await _userManager.FindByIdAsync(selectedUserId);

            if (user == null)
            {
                TempData["Error"] =
                    "The selected User account was not found.";

                return RedirectToAction(
                    nameof(ManageAccount),
                    new { id = employeeId });
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("User"))
            {
                TempData["Error"] =
                    "Only User accounts can be linked to employees.";

                return RedirectToAction(
                    nameof(ManageAccount),
                    new { id = employeeId });
            }

            var alreadyLinked = await _context.Employees
                .AnyAsync(e =>
                    e.IdentityUserId == selectedUserId &&
                    e.EmployeeId != employeeId);

            if (alreadyLinked)
            {
                TempData["Error"] =
                    "This User account is already linked to another employee.";

                return RedirectToAction(
                    nameof(ManageAccount),
                    new { id = employeeId });
            }

            employee.IdentityUserId = selectedUserId;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Login account linked successfully.";

            return RedirectToAction(
                nameof(ManageAccount),
                new { id = employeeId });
        }


        // POST: Employees/UnlinkAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> UnlinkAccount(int employeeId)
        {
            if (employeeId <= 0)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound();
            }

            employee.IdentityUserId = null;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Login account unlinked successfully.";

            return RedirectToAction(
                nameof(ManageAccount),
                new { id = employeeId });
        }


        // GET: Employees/Delete/5
        [Authorize(Roles = "Admin,HR")]
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
        [Authorize(Roles = "Admin,HR")]
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