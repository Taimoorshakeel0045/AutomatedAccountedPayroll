
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomatedAccountedPayroll.Models
{
    public class Payroll
    {
        public int PayrollId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime PayrollMonth { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Allowances { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Overtime")]
        [Range(0, double.MaxValue, ErrorMessage = "Overtime cannot be negative.")]
        public decimal Overtime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Bonus")]
        [Range(0, double.MaxValue, ErrorMessage = "Bonus cannot be negative.")]
        public decimal Bonus { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Deductions cannot be negative.")]
        public decimal Deductions { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Loan Deduction")]
        [Range(0, double.MaxValue, ErrorMessage = "Loan deduction cannot be negative.")]
        public decimal LoanDeduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Advance Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Advance salary cannot be negative.")]
        public decimal AdvanceSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; }

        [StringLength(30)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        [Display(Name = "Payment Reference")]
        public string? PaymentReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Employee? Employee { get; set; }
    }
}
