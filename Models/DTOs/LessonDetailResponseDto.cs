using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class LessonDetailResponseDto
    {
        public LessonResponseDto Lesson { get; set; }
        public List<LessonContentResponseDto> Contents { get; set; }
    }
}
