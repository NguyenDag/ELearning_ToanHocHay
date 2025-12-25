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

        // Navigation
        public User? User { get; set; }

        public ICollection<StudentParent> StudentParents { get; set; }
    }
}
