using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutomatedAccountedPayroll.Models
{
    public class LoanPayment
    {
        public int LoanPaymentId { get; set; }

        [Required]
        [Display(Name = "Loan")]
        public int LoanId { get; set; }

        [ForeignKey("LoanId")]
        public Loan? Loan { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000000)]
        [Display(Name = "Payment Amount")]
        public decimal PaymentAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Remaining Balance")]
        public decimal RemainingBalance { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Payroll Deduction";

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}