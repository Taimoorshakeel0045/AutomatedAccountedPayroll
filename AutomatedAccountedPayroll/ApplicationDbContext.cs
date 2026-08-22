using AutomatedAccountedPayroll.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace AutomatedAccountedPayroll.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Designation> Designations { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Payroll> Payrolls { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Leave> Leaves { get; set; }

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



            // working hours , late time 
            builder.Entity<Attendance>()
    .HasIndex(a => new
    {
        a.EmployeeId,
        a.AttendanceDate
    })
    .IsUnique();





            // Department → Designation relationship
            builder.Entity<Designation>()
                    .HasOne(d => d.Department)
                    .WithMany(d => d.Designations)
                    .HasForeignKey(d => d.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

               //db proction for gesignation duplication
            
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
        }
    }
}