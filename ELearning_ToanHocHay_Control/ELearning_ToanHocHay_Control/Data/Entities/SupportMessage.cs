using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("SupportMessage")]
    public class SupportMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int TicketId { get; set; }
        public int SenderUserId { get; set; }

        [Required]
        public string MessageText { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public SupportTicket? Ticket { get; set; }
        public User? Sender { get; set; }
    }
}
