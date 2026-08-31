using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    // ============================================================
    // TẦNG 4 — KỸ NĂNG (Giai đoạn 2 — dựng bảng trước, chưa bắt buộc dữ liệu)
    // ============================================================

    [Table("Skill")]
    public class Skill
    {
        [Key]
        public int SkillId { get; set; }

        public int SubjectId { get; set; }
        public int? ParentSkillId { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        public string? Description { get; set; }

        // Navigation
        public Subject? Subject { get; set; }
        public Skill? Parent { get; set; }
        public ICollection<Skill> Children { get; set; }
        public ICollection<NodeSkill> NodeSkills { get; set; }
        public ICollection<QuestionSkill> QuestionSkills { get; set; }
        public ICollection<SkillProgress> SkillProgresses { get; set; }
    }

    [Table("NodeSkill")]
    public class NodeSkill
    {
        public int NodeId { get; set; }
        public int SkillId { get; set; }

        // Navigation
        public ContentNode? Node { get; set; }
        public Skill? Skill { get; set; }
    }

    [Table("QuestionSkill")]
    public class QuestionSkill
    {
        public int QuestionId { get; set; }
        public int SkillId { get; set; }

        public double Weight { get; set; } = 1;

        // Navigation
        public Question? Question { get; set; }
        public Skill? Skill { get; set; }
    }
}
