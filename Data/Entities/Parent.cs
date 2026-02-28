using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    [Table("Parent")]
    public class Parent
    {
        [Key]
        public int ParentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string? Job { get; set; }

        [Required, MaxLength(20)]
        public string ConnectionCode { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        // Navigation
        public User? User { get; set; }

        public ICollection<StudentParent> StudentParents { get; set; }
    }
}
