using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum TicketStatus
    {
        Open,
        InProgress,
        Resolved,
        Closed
    }

    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    [Table("SupportTicket")]
    public class SupportTicket
    {
        [Key]
        public int TicketId { get; set; }

        public int CreatedByUserId { get; set; }
        public int? AssignedToStaffId { get; set; }

        [Required, MaxLength(255)]
        public string Subject { get; set; }

        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }

        // Navigation
        public User? CreatedBy { get; set; }
        public User? AssignedStaff { get; set; }

        public ICollection<SupportMessage> Messages { get; set; }
    }
}
