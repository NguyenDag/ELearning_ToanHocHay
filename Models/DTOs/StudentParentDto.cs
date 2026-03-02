using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class StudentParentDto
    {
        public int StudentId { get; set; }
        public int ParentId { get; set; }
        public ParentRelationship Relationship { get; set; }

        public string ParentName { get; set; }
        public string ParentEmail { get; set; }
    }
}
