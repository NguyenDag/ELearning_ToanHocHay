using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    /// <summary>
    /// P6 — per-user opt-out for a notification rule. A missing row means "enabled".
    /// RuleKey values: "tab-switch", "low-score", "inactivity".
    /// </summary>
    [Table("NotificationPreference")]
    public class NotificationPreference
    {
        public int UserId { get; set; }

        [MaxLength(40)]
        public string RuleKey { get; set; } = "";

        public bool Enabled { get; set; } = true;

        // Navigation
        public User? User { get; set; }
    }
}
