using AutomatedAccountedPayroll.Data;
using AutomatedAccountedPayroll.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AutomatedAccountedPayroll.Services
{
    public class AuditLogAuthenticationEvents
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogAuthenticationEvents(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, ClaimsPrincipal? principal)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = principal?.Identity?.IsAuthenticated == true
                ? principal.Identity.Name
                : "Unknown";

            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
            if (userAgent?.Length > 1000)
            {
                userAgent = userAgent[..1000];
            }

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = "Authentication",
                EntityId = userId,
                Changes = null,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                RequestPath = httpContext?.Request.Path.Value,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}
