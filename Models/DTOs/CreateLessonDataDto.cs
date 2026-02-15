using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Models.DTOs.Chapter;
using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;
using ELearning_ToanHocHay_Control.Models.DTOs.Topic;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class CreateLessonDataDto
    {
        public CreateChapterDto Chapter { get; set; }
        public CreateTopicDto Topic { get; set; }

        [Required(ErrorMessage = "Lesson là bắt buộc")]
        public CreateLessonDto Lesson { get; set; }

        [Required(ErrorMessage = "LessonContents là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 content block")]
        public List<CreateLessonContentDto> LessonContents { get; set; }
    }
}
