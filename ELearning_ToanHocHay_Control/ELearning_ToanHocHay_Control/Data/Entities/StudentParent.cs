using System.ComponentModel.DataAnnotations.Schema;

namespace ELearning_ToanHocHay_Control.Data.Entities
{
    public enum ParentRelationship
    {
        Father,
        Mother,
        Guardian,
        Other
    }

    [Table("StudentParent")]
    public class StudentParent
    {
        public int StudentId { get; set; }
        public int ParentId { get; set; }

        public ParentRelationship Relationship { get; set; }

        // Navigation
        public Student? Student { get; set; }
        public Parent? Parent { get; set; }
    }
}
