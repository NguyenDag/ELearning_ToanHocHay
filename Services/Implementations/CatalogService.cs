using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Catalog;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class CatalogService : ICatalogService
    {
        private readonly ICatalogRepository _repo;

        public CatalogService(ICatalogRepository repo)
        {
            _repo = repo;
        }

        // ================= Subject =================
        public async Task<ApiResponse<List<SubjectDto>>> GetSubjectsAsync(bool includeInactive)
        {
            var items = await _repo.GetSubjectsAsync(includeInactive);
            return ApiResponse<List<SubjectDto>>.SuccessResponse(items.Select(Map).ToList());
        }

        public async Task<ApiResponse<SubjectDto>> GetSubjectAsync(int id)
        {
            var s = await _repo.GetSubjectAsync(id);
            return s == null
                ? ApiResponse<SubjectDto>.ErrorResponse("Subject not found")
                : ApiResponse<SubjectDto>.SuccessResponse(Map(s));
        }

        public async Task<ApiResponse<SubjectDto>> CreateSubjectAsync(SubjectRequestDto dto)
        {
            if (await _repo.SubjectCodeExistsAsync(dto.Code))
                return ApiResponse<SubjectDto>.ErrorResponse($"Subject code '{dto.Code}' already exists");

            var s = new Subject
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Slug = dto.Slug.Trim(),
                Description = dto.Description,
                IconUrl = dto.IconUrl,
                ColorHex = dto.ColorHex,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive
            };
            await _repo.AddSubjectAsync(s);
            return ApiResponse<SubjectDto>.SuccessResponse(Map(s), "Subject created");
        }

        public async Task<ApiResponse<SubjectDto>> UpdateSubjectAsync(int id, SubjectRequestDto dto)
        {
            var s = await _repo.GetSubjectAsync(id);
            if (s == null) return ApiResponse<SubjectDto>.ErrorResponse("Subject not found");

            if (await _repo.SubjectCodeExistsAsync(dto.Code, id))
                return ApiResponse<SubjectDto>.ErrorResponse($"Subject code '{dto.Code}' already exists");

            s.Code = dto.Code.Trim();
            s.Name = dto.Name.Trim();
            s.Slug = dto.Slug.Trim();
            s.Description = dto.Description;
            s.IconUrl = dto.IconUrl;
            s.ColorHex = dto.ColorHex;
            s.DisplayOrder = dto.DisplayOrder;
            s.IsActive = dto.IsActive;
            await _repo.UpdateSubjectAsync(s);
            return ApiResponse<SubjectDto>.SuccessResponse(Map(s), "Subject updated");
        }

        // ================= GradeLevel =================
        public async Task<ApiResponse<List<GradeLevelDto>>> GetGradeLevelsAsync(bool includeInactive)
        {
            var items = await _repo.GetGradeLevelsAsync(includeInactive);
            return ApiResponse<List<GradeLevelDto>>.SuccessResponse(items.Select(Map).ToList());
        }

        public async Task<ApiResponse<GradeLevelDto>> GetGradeLevelAsync(int id)
        {
            var g = await _repo.GetGradeLevelAsync(id);
            return g == null
                ? ApiResponse<GradeLevelDto>.ErrorResponse("Grade level not found")
                : ApiResponse<GradeLevelDto>.SuccessResponse(Map(g));
        }

        public async Task<ApiResponse<GradeLevelDto>> CreateGradeLevelAsync(GradeLevelRequestDto dto)
        {
            if (await _repo.GradeLevelCodeExistsAsync(dto.Code))
                return ApiResponse<GradeLevelDto>.ErrorResponse($"Grade level code '{dto.Code}' already exists");

            var g = new GradeLevel
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Stage = dto.Stage,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive
            };
            await _repo.AddGradeLevelAsync(g);
            return ApiResponse<GradeLevelDto>.SuccessResponse(Map(g), "Grade level created");
        }

        public async Task<ApiResponse<GradeLevelDto>> UpdateGradeLevelAsync(int id, GradeLevelRequestDto dto)
        {
            var g = await _repo.GetGradeLevelAsync(id);
            if (g == null) return ApiResponse<GradeLevelDto>.ErrorResponse("Grade level not found");

            if (await _repo.GradeLevelCodeExistsAsync(dto.Code, id))
                return ApiResponse<GradeLevelDto>.ErrorResponse($"Grade level code '{dto.Code}' already exists");

            g.Code = dto.Code.Trim();
            g.Name = dto.Name.Trim();
            g.Stage = dto.Stage;
            g.DisplayOrder = dto.DisplayOrder;
            g.IsActive = dto.IsActive;
            await _repo.UpdateGradeLevelAsync(g);
            return ApiResponse<GradeLevelDto>.SuccessResponse(Map(g), "Grade level updated");
        }

        // ================= CurriculumFramework =================
        public async Task<ApiResponse<List<FrameworkDto>>> GetFrameworksAsync(bool includeInactive)
        {
            var items = await _repo.GetFrameworksAsync(includeInactive);
            return ApiResponse<List<FrameworkDto>>.SuccessResponse(items.Select(Map).ToList());
        }

        public async Task<ApiResponse<FrameworkDto>> GetFrameworkAsync(int id)
        {
            var f = await _repo.GetFrameworkAsync(id);
            return f == null
                ? ApiResponse<FrameworkDto>.ErrorResponse("Framework not found")
                : ApiResponse<FrameworkDto>.SuccessResponse(Map(f));
        }

        public async Task<ApiResponse<FrameworkDto>> CreateFrameworkAsync(FrameworkRequestDto dto)
        {
            if (await _repo.FrameworkCodeExistsAsync(dto.Code))
                return ApiResponse<FrameworkDto>.ErrorResponse($"Framework code '{dto.Code}' already exists");

            var f = new CurriculumFramework
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Publisher = dto.Publisher,
                IsActive = dto.IsActive
            };
            await _repo.AddFrameworkAsync(f);
            return ApiResponse<FrameworkDto>.SuccessResponse(Map(f), "Framework created");
        }

        public async Task<ApiResponse<FrameworkDto>> UpdateFrameworkAsync(int id, FrameworkRequestDto dto)
        {
            var f = await _repo.GetFrameworkAsync(id);
            if (f == null) return ApiResponse<FrameworkDto>.ErrorResponse("Framework not found");

            if (await _repo.FrameworkCodeExistsAsync(dto.Code, id))
                return ApiResponse<FrameworkDto>.ErrorResponse($"Framework code '{dto.Code}' already exists");

            f.Code = dto.Code.Trim();
            f.Name = dto.Name.Trim();
            f.Publisher = dto.Publisher;
            f.IsActive = dto.IsActive;
            await _repo.UpdateFrameworkAsync(f);
            return ApiResponse<FrameworkDto>.SuccessResponse(Map(f), "Framework updated");
        }

        // ================= mapping =================
        private static SubjectDto Map(Subject s) => new()
        {
            SubjectId = s.SubjectId,
            Code = s.Code,
            Name = s.Name,
            Slug = s.Slug,
            Description = s.Description,
            IconUrl = s.IconUrl,
            ColorHex = s.ColorHex,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive
        };

        private static GradeLevelDto Map(GradeLevel g) => new()
        {
            GradeLevelId = g.GradeLevelId,
            Code = g.Code,
            Name = g.Name,
            Stage = g.Stage,
            DisplayOrder = g.DisplayOrder,
            IsActive = g.IsActive
        };

        private static FrameworkDto Map(CurriculumFramework f) => new()
        {
            FrameworkId = f.FrameworkId,
            Code = f.Code,
            Name = f.Name,
            Publisher = f.Publisher,
            IsActive = f.IsActive
        };
    }
}
