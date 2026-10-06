using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutomatedAccountedPayroll.ViewModels
{
    public class EmployeeAccountViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string EmployeeCode { get; set; } = string.Empty;

        public string? CurrentUserId { get; set; }

        public string? CurrentUserEmail { get; set; }

        public string? SelectedUserId { get; set; }

        public List<SelectListItem> AvailableUsers { get; set; } = new();
    }
}