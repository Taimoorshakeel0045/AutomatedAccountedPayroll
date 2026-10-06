using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace AutomatedAccountedPayroll.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Designation> Designations { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Payroll> Payrolls { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Leave> Leaves { get; set; }

        public DbSet<LeaveBalance> LeaveBalances { get; set; }

        public DbSet<Loan> Loans { get; set; }

        public DbSet<LoanPayment> LoanPayments { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public override int SaveChanges()
        {
            AddAuditLogs();

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            AddAuditLogs();

            return await base.SaveChangesAsync(cancellationToken);
        }

        private void AddAuditLogs()
        {
            ChangeTracker.DetectChanges();

            var httpContext = _httpContextAccessor.HttpContext;

            var userId = httpContext?.User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var userName = httpContext?.User?.Identity?.IsAuthenticated == true
                ? httpContext.User.Identity.Name
                : "System";

            var ipAddress =
                httpContext?.Connection.RemoteIpAddress?.ToString();

            var requestPath =
                httpContext?.Request.Path.Value;

            var userAgent =
                httpContext?.Request.Headers.UserAgent.ToString();

            if (userAgent?.Length > 1000)
            {
                userAgent = userAgent[..1000];
            }

            var auditLogs = new List<AuditLog>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog)
                {
                    continue;
                }

                if (entry.State != EntityState.Added &&
                    entry.State != EntityState.Modified &&
                    entry.State != EntityState.Deleted)
                {
                    continue;
                }

                var action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => "Unknown"
                };

                var entityName = entry.Metadata.ClrType.Name;

                var entityId = GetEntityId(entry);

                var changes = GetChanges(entry);

                auditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Changes = changes,
                    IpAddress = ipAddress,
                    RequestPath = requestPath,
                    UserAgent = userAgent,
                    Timestamp = DateTime.UtcNow
                });
            }

            if (auditLogs.Count > 0)
            {
                AuditLogs.AddRange(auditLogs);
            }
        }

        private static string? GetEntityId(EntityEntry entry)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();

            if (primaryKey == null)
            {
                return null;
            }

            var values = primaryKey.Properties
                .Select(property =>
                {
                    var propertyEntry = entry.Property(property.Name);

                    return entry.State == EntityState.Deleted
                        ? propertyEntry.OriginalValue?.ToString()
                        : propertyEntry.CurrentValue?.ToString();
                })
                .Where(value => value != null);

            return string.Join(",", values);
        }

        private static string? GetChanges(EntityEntry entry)
        {
            var changes = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;

                if (IsSensitiveProperty(propertyName))
                {
                    continue;
                }

                if (entry.State == EntityState.Added)
                {
                    changes[propertyName] = property.CurrentValue;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    changes[propertyName] = new
                    {
                        OldValue = property.OriginalValue
                    };
                }
                else if (entry.State == EntityState.Modified &&
                         property.IsModified &&
                         !Equals(
                             property.OriginalValue,
                             property.CurrentValue))
                {
                    changes[propertyName] = new
                    {
                        OldValue = property.OriginalValue,
                        NewValue = property.CurrentValue
                    };
                }
            }

            if (changes.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(changes);
        }

        private static bool IsSensitiveProperty(string propertyName)
        {
            return propertyName.Equals(
                       "PasswordHash",
                       StringComparison.OrdinalIgnoreCase)
                   || propertyName.Equals(
                       "SecurityStamp",
                       StringComparison.OrdinalIgnoreCase)
                   || propertyName.Equals(
                       "ConcurrencyStamp",
                       StringComparison.OrdinalIgnoreCase);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Employee>()
                .Property(e => e.BasicSalary)
                .HasPrecision(18, 2);

            builder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();

            builder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employee>()
                .HasOne(e => e.Designation)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Employee>()
                .Property(e => e.IdentityUserId)
                .HasMaxLength(450);

            builder.Entity<Employee>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(e => e.IdentityUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Employee>()
                .HasIndex(e => e.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");

            builder.Entity<Attendance>()
                .Property(a => a.WorkingHours)
                .HasPrecision(5, 2);

            builder.Entity<Leave>()
                .Property(l => l.TotalDays)
                .HasPrecision(5, 2);

            builder.Entity<Leave>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LeaveBalance>()
                .HasIndex(lb => new
                {
                    lb.EmployeeId,
                    lb.Year,
                    lb.LeaveType
                })
                .IsUnique();

            builder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.EmployeeId,
                    a.AttendanceDate
                })
                .IsUnique();

            builder.Entity<Designation>()
                .HasOne(d => d.Department)
                .WithMany(d => d.Designations)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Designation>()
                .HasIndex(d => new
                {
                    d.DepartmentId,
                    d.DesignationName
                })
                .IsUnique();

            builder.Entity<Payroll>()
                .HasIndex(p => new
                {
                    p.EmployeeId,
                    p.PayrollMonth
                })
                .IsUnique();

            builder.Entity<Loan>()
                .Property(l => l.LoanAmount)
                .HasPrecision(18, 2);

            builder.Entity<Loan>()
                .Property(l => l.MonthlyInstallment)
                .HasPrecision(18, 2);

            builder.Entity<Loan>()
                .Property(l => l.RemainingBalance)
                .HasPrecision(18, 2);

            builder.Entity<Loan>()
                .HasOne(l => l.Employee)
                .WithMany()
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LoanPayment>()
                .Property(p => p.PaymentAmount)
                .HasPrecision(18, 2);

            builder.Entity<LoanPayment>()
                .Property(p => p.RemainingBalance)
                .HasPrecision(18, 2);

            builder.Entity<LoanPayment>()
                .HasOne(p => p.Loan)
                .WithMany()
                .HasForeignKey(p => p.LoanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}