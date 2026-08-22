namespace AutomatedAccountedPayroll.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();

        public ICollection<Designation> Designations { get; set; } = new List<Designation>();
    }
}
