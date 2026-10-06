using System.ComponentModel.DataAnnotations;

namespace AutomatedAccountedPayroll.Models
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string EntityName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        public string? Changes { get; set; }

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? RequestPath { get; set; }

        [MaxLength(1000)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
