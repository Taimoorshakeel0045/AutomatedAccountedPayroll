using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomatedAccountedPayroll.Models
{
    public class LeaveBalance
    {
        public int LeaveBalanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(30)]
        public string LeaveType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal AllocatedDays { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal UsedDays { get; set; }
    }
}