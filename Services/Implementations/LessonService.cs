using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class LessonService : ILessonSevice
    {
        public Task<ApiResponse<LessonDto>> CreateAsync(CreateLessonDto dto, int creatorId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<LessonDto>> GetByIdAsync(int lessonId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<LessonDto>>> GetByTopicAsync(int topicId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<bool>> PublishAsync(int lessonId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<bool>> ReviewAsync(int lessonId, ReviewLessonDto dto, int reviewerId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<bool>> SubmitForReviewAsync(int lessonId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<LessonDto>> UpdateAsync(int lessonId, UpdateLessonDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
