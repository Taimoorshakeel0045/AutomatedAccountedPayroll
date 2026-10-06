using AutomatedAccountedPayroll.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutomatedAccountedPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToListAsync();

            ViewBag.TotalInDb = logs.Count;
            ViewBag.Actions = new List<string>();
            ViewBag.EntityNames = new List<string>();
            ViewBag.UserName = null;
            ViewBag.Action = null;
            ViewBag.EntityName = null;
            ViewBag.FromDate = null;
            ViewBag.ToDate = null;

            return View(logs);
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var auditLog = await _context.AuditLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AuditLogId == id.Value);

            if (auditLog == null)
            {
                return NotFound();
            }

            return View(auditLog);
        }
    }
}
