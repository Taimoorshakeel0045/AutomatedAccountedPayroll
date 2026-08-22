using System.ComponentModel.DataAnnotations;

namespace AutomatedAccountedPayroll.Models
{
    public class Designation
    {
        public int DesignationId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Designation Name")]
        public string DesignationName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        //[Required]
        [Display(Name = "Department")]
        //public int DepartmentId { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;

        public Department? Department { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
