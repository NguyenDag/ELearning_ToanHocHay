using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ConnectParentDto
    {
        [Required]
        public required string ConnectionCode { get; set; }

        [Required]
        public ParentRelationship Relationship { get; set; }
    }
}
