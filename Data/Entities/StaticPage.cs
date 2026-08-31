using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // TRANG TĨNH ("giới thiệu hệ thống"…) — độc lập, làm bất cứ lúc nào
    // ============================================================

    [Table("StaticPage")]
    public class StaticPage
    {
        [Key]
        public int StaticPageId { get; set; }

        [Required, MaxLength(150)]
        public string Slug { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        public string? BodyHtml { get; set; }

        public bool IsPublished { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
