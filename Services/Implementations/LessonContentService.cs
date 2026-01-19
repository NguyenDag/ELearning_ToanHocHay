using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class LessonContentService : ILessonContentService
    {
        private readonly ILessonContentRepository _repository;

        public LessonContentService(ILessonContentRepository repository)
        {
            _repository = repository;
        }
        public Task<ApiResponse<LessonContentDto>> CreateAsync(int lessonId, CreateLessonContentDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<bool>> DeleteAsync(int contentId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<LessonContentDto>> GetByIdAsync(int contentId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse<IEnumerable<LessonContentDto>>> GetByLessonAsync(int lessonId)
        {
            var contents = await _repository.GetByLessonAsync(lessonId);

            return ApiResponse<IEnumerable<LessonContentDto>>
                .SuccessResponse(contents);
        }

        public Task<ApiResponse<LessonContentDto>> UpdateAsync(int contentId, CreateLessonContentDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
