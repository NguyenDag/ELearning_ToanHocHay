using ELearning_ToanHocHay_Control.Models.DTOs.Chapter;
using ELearning_ToanHocHay_Control.Models.DTOs.Lesson;
using ELearning_ToanHocHay_Control.Models.DTOs.LessonContent;
using ELearning_ToanHocHay_Control.Models.DTOs.Topic;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class CreateOrAddLessonDataDto
    {
        public UpsertChapterDto? Chapter { get; set; }
        public UpsertTopicDto? Topic { get; set; }
        public UpsertLessonDto Lesson { get; set; }
        public List<CreateLessonContentDto> LessonContents { get; set; }
    }
}
