using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum IpnOutcome
    {
        Received,
        Processed,
        Duplicate,
        Ignored,
        AmountMismatch,
        Error
    }

    /// <summary>
    /// P5 (A2-10) — every SePay IPN callback, stored raw. ReferenceCode is unique so a
    /// replayed transaction is idempotent at the transaction level, not just per subscription.
    /// </summary>
    [Table("SePayIpnLog")]
    public class SePayIpnLog
    {
        [Key]
        public long IpnLogId { get; set; }

        /// <summary>SePay's per-transaction reference — unique.</summary>
        [Required, MaxLength(120)]
        public string ReferenceCode { get; set; } = "";

        [Column(TypeName = "text")]
        public string RawPayload { get; set; } = "";

        public int? SubscriptionId { get; set; }

        public long TransferAmount { get; set; }

        [MaxLength(10)]
        public string? TransferType { get; set; }

        public IpnOutcome Outcome { get; set; } = IpnOutcome.Received;

        [MaxLength(500)]
        public string? ResultMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
    }
}
