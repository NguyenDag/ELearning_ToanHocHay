using ELearning_ToanHocHay_Control.Models.DTOs;

namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    public interface ITopicService
    {
        Task<ApiResponse<IEnumerable<TopicDto>>> GetByChapterAsync(int chapterId);
        Task<ApiResponse<TopicDto>> GetByIdAsync(int topicId);
        Task<ApiResponse<TopicDto>> CreateAsync(CreateTopicDto dto);
        Task<ApiResponse<TopicDto>> UpdateAsync(int topicId, UpdateTopicDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int topicId);
    }
}
