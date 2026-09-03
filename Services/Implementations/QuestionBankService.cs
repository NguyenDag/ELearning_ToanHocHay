using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Question;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class QuestionBankService : IQuestionBankService
    {
        private readonly IQuestionBankRepository _bankRepo;
        private readonly IQuestionRepository _questionRepo;
        private readonly ICatalogRepository _catalog;

        public QuestionBankService(
            IQuestionBankRepository bankRepo, IQuestionRepository questionRepo, ICatalogRepository catalog)
        {
            _bankRepo = bankRepo;
            _questionRepo = questionRepo;
            _catalog = catalog;
        }

        // ================= banks =================
        public async Task<ApiResponse<List<QuestionBankDto>>> GetBanksAsync(int? subjectId, int? gradeLevelId, bool includeInactive)
        {
            var banks = await _bankRepo.GetAllAsync(subjectId, gradeLevelId, includeInactive);
            var dtos = new List<QuestionBankDto>();
            foreach (var b in banks)
                dtos.Add(Map(b, await _bankRepo.QuestionCountAsync(b.BankId)));
            return ApiResponse<List<QuestionBankDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponse<QuestionBankDto>> GetBankAsync(int bankId)
        {
            var b = await _bankRepo.GetQuestionBankByIdAsync(bankId);
            return b == null
                ? ApiResponse<QuestionBankDto>.ErrorResponse("Question bank not found")
                : ApiResponse<QuestionBankDto>.SuccessResponse(Map(b, await _bankRepo.QuestionCountAsync(bankId)));
        }

        public async Task<ApiResponse<QuestionBankDto>> CreateBankAsync(QuestionBankRequestDto dto, int userId)
        {
            var err = await ValidateRefsAsync(dto);
            if (err != null) return ApiResponse<QuestionBankDto>.ErrorResponse(err);

            var bank = new QuestionBank
            {
                BankName = dto.BankName.Trim(),
                Description = dto.Description,
                SubjectId = dto.SubjectId,
                GradeLevelId = dto.GradeLevelId,
                CourseId = dto.CourseId,
                PrimaryNodeId = dto.PrimaryNodeId,
                CreatedBy = userId,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            await _bankRepo.CreateQuestionBankAsync(bank);
            return ApiResponse<QuestionBankDto>.SuccessResponse(Map(bank, 0), "Question bank created");
        }

        public async Task<ApiResponse<QuestionBankDto>> UpdateBankAsync(int bankId, QuestionBankRequestDto dto)
        {
            var bank = await _bankRepo.GetQuestionBankByIdAsync(bankId);
            if (bank == null) return ApiResponse<QuestionBankDto>.ErrorResponse("Question bank not found");

            var err = await ValidateRefsAsync(dto);
            if (err != null) return ApiResponse<QuestionBankDto>.ErrorResponse(err);

            bank.BankName = dto.BankName.Trim();
            bank.Description = dto.Description;
            bank.SubjectId = dto.SubjectId;
            bank.GradeLevelId = dto.GradeLevelId;
            bank.CourseId = dto.CourseId;
            bank.PrimaryNodeId = dto.PrimaryNodeId;
            bank.IsActive = dto.IsActive;
            await _bankRepo.UpdateQuestionBankAsync(bank);
            return ApiResponse<QuestionBankDto>.SuccessResponse(
                Map(bank, await _bankRepo.QuestionCountAsync(bankId)), "Question bank updated");
        }

        public async Task<ApiResponse<bool>> DeleteBankAsync(int bankId)
        {
            if (await _bankRepo.GetQuestionBankByIdAsync(bankId) == null)
                return ApiResponse<bool>.ErrorResponse("Question bank not found");
            if (await _bankRepo.QuestionCountAsync(bankId) > 0)
                return ApiResponse<bool>.ErrorResponse("Delete or move the questions first");

            await _bankRepo.DeleteQuestionBankAsync(bankId);
            return ApiResponse<bool>.SuccessResponse(true, "Question bank deleted");
        }

        // ================= questions =================
        public async Task<ApiResponse<PagedResult<QuestionAdminDto>>> GetQuestionsAsync(
            int bankId, QuestionStatus? status, string? search, int page, int pageSize)
        {
            if (await _bankRepo.GetQuestionBankByIdAsync(bankId) == null)
                return ApiResponse<PagedResult<QuestionAdminDto>>.ErrorResponse("Question bank not found");

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _questionRepo.GetByBankAsync(bankId, status, search, page, pageSize);
            return ApiResponse<PagedResult<QuestionAdminDto>>.SuccessResponse(new PagedResult<QuestionAdminDto>
            {
                Items = items.Select(Map).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<QuestionAdminDto>> GetQuestionAsync(int questionId)
        {
            var q = await _questionRepo.GetQuestionByIdAsync(questionId);
            return q == null
                ? ApiResponse<QuestionAdminDto>.ErrorResponse("Question not found")
                : ApiResponse<QuestionAdminDto>.SuccessResponse(Map(q));
        }

        public async Task<ApiResponse<QuestionAdminDto>> UpdateQuestionAsync(int questionId, UpdateQuestionDto dto)
        {
            var q = await _questionRepo.GetQuestionByIdAsync(questionId);
            if (q == null) return ApiResponse<QuestionAdminDto>.ErrorResponse("Question not found");
            if (q.Status == QuestionStatus.Approved)
                return ApiResponse<QuestionAdminDto>.ErrorResponse("An approved question cannot be edited — clone it instead");

            q.QuestionText = dto.QuestionText;
            q.QuestionImageUrl = dto.QuestionImageUrl;
            q.QuestionType = dto.QuestionType;
            q.DifficultyLevel = dto.DifficultyLevel;
            q.CorrectAnswer = dto.CorrectAnswer;
            q.Explanation = dto.Explanation;
            q.Status = QuestionStatus.Draft; // an edit invalidates a prior review
            q.RejectReason = null;
            q.UpdatedAt = DateTime.UtcNow;
            q.Version += 1;

            q.QuestionOptions?.Clear();
            q.QuestionOptions = dto.Options.Select(o => new QuestionOption
            {
                OptionText = o.OptionText,
                IsCorrect = o.IsCorrect,
                OrderIndex = o.OrderIndex
            }).ToList();

            await _questionRepo.SaveAsync();
            return ApiResponse<QuestionAdminDto>.SuccessResponse(Map(q), "Question updated");
        }

        public async Task<ApiResponse<bool>> DeleteQuestionAsync(int questionId)
        {
            var q = await _questionRepo.GetQuestionByIdAsync(questionId);
            if (q == null) return ApiResponse<bool>.ErrorResponse("Question not found");
            if (await _questionRepo.IsUsedInAttemptsAsync(questionId))
                return ApiResponse<bool>.ErrorResponse("Question already has student answers — deactivate it instead");

            await _questionRepo.DeleteAsync(q);
            return ApiResponse<bool>.SuccessResponse(true, "Question deleted");
        }

        // ================= review workflow =================
        public async Task<ApiResponse<QuestionAdminDto>> SubmitQuestionAsync(int questionId)
        {
            var q = await _questionRepo.GetQuestionByIdAsync(questionId);
            if (q == null) return ApiResponse<QuestionAdminDto>.ErrorResponse("Question not found");
            if (q.Status is not (QuestionStatus.Draft or QuestionStatus.Rejected))
                return ApiResponse<QuestionAdminDto>.ErrorResponse($"Cannot submit a {q.Status} question");

            q.Status = QuestionStatus.PendingReview;
            q.RejectReason = null;
            q.UpdatedAt = DateTime.UtcNow;
            await _questionRepo.SaveAsync();
            return ApiResponse<QuestionAdminDto>.SuccessResponse(Map(q), "Question submitted for review");
        }

        public async Task<ApiResponse<QuestionAdminDto>> ReviewQuestionAsync(int questionId, ReviewQuestionDto dto, int reviewerId)
        {
            var q = await _questionRepo.GetQuestionByIdAsync(questionId);
            if (q == null) return ApiResponse<QuestionAdminDto>.ErrorResponse("Question not found");
            if (q.Status != QuestionStatus.PendingReview)
                return ApiResponse<QuestionAdminDto>.ErrorResponse($"Only a PendingReview question can be reviewed (current: {q.Status})");

            if (dto.Approve)
            {
                q.Status = QuestionStatus.Approved;
                q.RejectReason = null;
                q.PublishedAt = DateTime.UtcNow;
            }
            else
            {
                q.Status = QuestionStatus.Rejected;
                q.RejectReason = dto.RejectReason;
            }
            q.ReviewedBy = reviewerId;
            q.ReviewedAt = DateTime.UtcNow;
            await _questionRepo.SaveAsync();
            return ApiResponse<QuestionAdminDto>.SuccessResponse(Map(q), dto.Approve ? "Question approved" : "Question rejected");
        }

        // ================= helpers =================
        private async Task<string?> ValidateRefsAsync(QuestionBankRequestDto dto)
        {
            if (await _catalog.GetSubjectAsync(dto.SubjectId) == null) return "Subject not found";
            if (await _catalog.GetGradeLevelAsync(dto.GradeLevelId) == null) return "Grade level not found";
            return null;
        }

        private static QuestionBankDto Map(QuestionBank b, int questionCount) => new()
        {
            BankId = b.BankId,
            BankName = b.BankName,
            Description = b.Description,
            SubjectId = b.SubjectId,
            SubjectName = b.Subject?.Name,
            GradeLevelId = b.GradeLevelId,
            GradeLevelName = b.GradeLevel?.Name,
            CourseId = b.CourseId,
            PrimaryNodeId = b.PrimaryNodeId,
            IsActive = b.IsActive,
            QuestionCount = questionCount,
            CreatedAt = b.CreatedAt
        };

        private static QuestionAdminDto Map(Question q) => new()
        {
            QuestionId = q.QuestionId,
            BankId = q.BankId,
            SubjectId = q.SubjectId,
            QuestionText = q.QuestionText,
            QuestionImageUrl = q.QuestionImageUrl,
            QuestionType = q.QuestionType,
            DifficultyLevel = q.DifficultyLevel,
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation,
            Status = q.Status,
            IsActive = q.IsActive,
            CreatedBy = q.CreatedBy,
            ReviewedBy = q.ReviewedBy,
            RejectReason = q.RejectReason,
            CreatedAt = q.CreatedAt,
            ReviewedAt = q.ReviewedAt,
            UpdatedAt = q.UpdatedAt,
            Options = (q.QuestionOptions ?? new List<QuestionOption>())
                .OrderBy(o => o.OrderIndex)
                .Select(o => new QuestionOptionDto { OptionId = o.OptionId, OptionText = o.OptionText, IsCorrect = o.IsCorrect })
                .ToList()
        };
    }
}
