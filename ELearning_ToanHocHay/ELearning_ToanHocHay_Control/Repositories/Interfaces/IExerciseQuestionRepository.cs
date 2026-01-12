using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IExerciseQuestionRepository
    {
        // Thêm 1 câu hỏi vào exercise
        Task AddAsync(ExerciseQuestion exerciseQuestion);

        // Thêm nhiều câu hỏi vào exercise
        Task AddRangeAsync(IEnumerable<ExerciseQuestion> exerciseQuestions);

        // Lấy tất cả câu hỏi của 1 exercise
        Task<List<ExerciseQuestion>> GetByExerciseIdAsync(int exerciseId);

        // Lấy 1 câu hỏi cụ thể trong exercise
        Task<ExerciseQuestion?> GetAsync(int exerciseId, int questionId);

        // Cập nhật điểm / thứ tự
        Task UpdateAsync(ExerciseQuestion exerciseQuestion);

        // Xóa 1 câu hỏi khỏi exercise
        Task RemoveAsync(int exerciseId, int questionId);

        // Xóa toàn bộ câu hỏi của exercise (khi edit đề)
        Task RemoveByExerciseIdAsync(int exerciseId);

        // Kiểm tra câu hỏi đã tồn tại trong exercise chưa
        Task<bool> ExistsAsync(int exerciseId, int questionId);
    }
}
