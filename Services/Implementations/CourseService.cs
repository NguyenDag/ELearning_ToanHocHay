using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Course;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _repo;
        private readonly ICatalogRepository _catalog;
        private readonly AppDbContext _context;

        public CourseService(ICourseRepository repo, ICatalogRepository catalog, AppDbContext context)
        {
            _repo = repo;
            _catalog = catalog;
            _context = context;
        }

        // ================= Course =================
        public async Task<ApiResponse<List<CourseDto>>> GetCoursesAsync(int? subjectId, int? gradeLevelId, bool publishedOnly)
        {
            var courses = await _repo.GetCoursesAsync(
                subjectId, gradeLevelId, publishedOnly ? CourseStatus.Published : null);
            return ApiResponse<List<CourseDto>>.SuccessResponse(courses.Select(c => Map(c, includeVersions: false)).ToList());
        }

        public async Task<ApiResponse<CourseDto>> GetCourseAsync(int courseId)
        {
            var c = await _repo.GetCourseAsync(courseId, withVersions: true);
            return c == null
                ? ApiResponse<CourseDto>.ErrorResponse("Course not found")
                : ApiResponse<CourseDto>.SuccessResponse(Map(c, includeVersions: true));
        }

        public async Task<ApiResponse<CourseDto>> GetCourseBySlugAsync(string slug)
        {
            var c = await _repo.GetCourseBySlugAsync(slug);
            return c == null
                ? ApiResponse<CourseDto>.ErrorResponse("Course not found")
                : ApiResponse<CourseDto>.SuccessResponse(Map(c, includeVersions: true));
        }

        public async Task<ApiResponse<CourseDto>> CreateCourseAsync(CourseRequestDto dto, int userId)
        {
            var validation = await ValidateCourseRefsAsync(dto);
            if (validation != null) return ApiResponse<CourseDto>.ErrorResponse(validation);

            if (await _repo.SlugExistsAsync(dto.Slug))
                return ApiResponse<CourseDto>.ErrorResponse($"Slug '{dto.Slug}' already exists");
            if (await _repo.SubjectGradeFrameworkExistsAsync(dto.SubjectId, dto.GradeLevelId, dto.FrameworkId))
                return ApiResponse<CourseDto>.ErrorResponse("A course for this subject / grade / framework already exists");

            var course = new Course
            {
                SubjectId = dto.SubjectId,
                GradeLevelId = dto.GradeLevelId,
                FrameworkId = dto.FrameworkId,
                Title = dto.Title.Trim(),
                Slug = dto.Slug.Trim(),
                Description = dto.Description,
                ThumbnailUrl = dto.ThumbnailUrl,
                Status = CourseStatus.Draft,
                ListPrice = dto.ListPrice,
                SalePrice = dto.SalePrice,
                IsPurchasable = dto.IsPurchasable,
                AccessDurationDays = dto.AccessDurationDays,
                DisplayOrder = dto.DisplayOrder,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddCourseAsync(course);

            var full = await _repo.GetCourseAsync(course.CourseId, withVersions: true);
            return ApiResponse<CourseDto>.SuccessResponse(Map(full!, includeVersions: true), "Course created");
        }

        public async Task<ApiResponse<CourseDto>> UpdateCourseAsync(int courseId, CourseRequestDto dto)
        {
            var course = await _repo.GetCourseAsync(courseId);
            if (course == null) return ApiResponse<CourseDto>.ErrorResponse("Course not found");

            var validation = await ValidateCourseRefsAsync(dto);
            if (validation != null) return ApiResponse<CourseDto>.ErrorResponse(validation);

            if (await _repo.SlugExistsAsync(dto.Slug, courseId))
                return ApiResponse<CourseDto>.ErrorResponse($"Slug '{dto.Slug}' already exists");
            if (await _repo.SubjectGradeFrameworkExistsAsync(dto.SubjectId, dto.GradeLevelId, dto.FrameworkId, courseId))
                return ApiResponse<CourseDto>.ErrorResponse("A course for this subject / grade / framework already exists");

            course.SubjectId = dto.SubjectId;
            course.GradeLevelId = dto.GradeLevelId;
            course.FrameworkId = dto.FrameworkId;
            course.Title = dto.Title.Trim();
            course.Slug = dto.Slug.Trim();
            course.Description = dto.Description;
            course.ThumbnailUrl = dto.ThumbnailUrl;
            course.ListPrice = dto.ListPrice;
            course.SalePrice = dto.SalePrice;
            course.IsPurchasable = dto.IsPurchasable;
            course.AccessDurationDays = dto.AccessDurationDays;
            course.DisplayOrder = dto.DisplayOrder;
            course.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateCourseAsync(course);

            var full = await _repo.GetCourseAsync(courseId, withVersions: true);
            return ApiResponse<CourseDto>.SuccessResponse(Map(full!, includeVersions: true), "Course updated");
        }

        public async Task<ApiResponse<CourseDto>> SetCourseArchivedAsync(int courseId, bool archived)
        {
            var course = await _repo.GetCourseAsync(courseId, withVersions: true);
            if (course == null) return ApiResponse<CourseDto>.ErrorResponse("Course not found");

            if (archived)
            {
                course.Status = CourseStatus.Archived;
            }
            else
            {
                course.Status = course.Versions != null && course.Versions.Any(v => v.State == VersionState.Published)
                    ? CourseStatus.Published
                    : CourseStatus.Draft;
            }
            course.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateCourseAsync(course);
            return ApiResponse<CourseDto>.SuccessResponse(Map(course, includeVersions: true),
                archived ? "Course archived" : "Course unarchived");
        }

        // ================= CourseVersion =================
        public async Task<ApiResponse<List<CourseVersionDto>>> GetVersionsAsync(int courseId)
        {
            if (await _repo.GetCourseAsync(courseId) == null)
                return ApiResponse<List<CourseVersionDto>>.ErrorResponse("Course not found");
            var versions = await _repo.GetVersionsAsync(courseId);
            return ApiResponse<List<CourseVersionDto>>.SuccessResponse(versions.Select(Map).ToList());
        }

        public async Task<ApiResponse<CourseVersionDto>> CreateVersionAsync(int courseId, CreateCourseVersionDto dto, int userId)
        {
            var course = await _repo.GetCourseAsync(courseId);
            if (course == null) return ApiResponse<CourseVersionDto>.ErrorResponse("Course not found");

            if (dto.CloneFromVersionId.HasValue)
            {
                var src = await _repo.GetVersionAsync(dto.CloneFromVersionId.Value);
                if (src == null || src.CourseId != courseId)
                    return ApiResponse<CourseVersionDto>.ErrorResponse("Source version does not belong to this course");
            }

            var version = new CourseVersion
            {
                CourseId = courseId,
                VersionNumber = await _repo.NextVersionNumberAsync(courseId),
                Label = dto.Label,
                State = VersionState.Draft,
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddVersionAsync(version);

            if (dto.CloneFromVersionId.HasValue)
                await _repo.CloneContentTreeAsync(dto.CloneFromVersionId.Value, version.CourseVersionId, userId);

            return ApiResponse<CourseVersionDto>.SuccessResponse(Map(version), "Version created");
        }

        public async Task<ApiResponse<CourseVersionDto>> SubmitVersionAsync(int versionId, int userId)
        {
            var v = await _repo.GetVersionAsync(versionId);
            if (v == null) return ApiResponse<CourseVersionDto>.ErrorResponse("Version not found");
            if (v.State != VersionState.Draft)
                return ApiResponse<CourseVersionDto>.ErrorResponse($"Only a Draft version can be submitted (current: {v.State})");

            var hasNodes = await _context.ContentNodes.AnyAsync(n => n.CourseVersionId == versionId);
            if (!hasNodes)
                return ApiResponse<CourseVersionDto>.ErrorResponse("Cannot submit an empty version — add content first");

            v.State = VersionState.InReview;
            v.SubmittedBy = userId;
            v.SubmittedAt = DateTime.UtcNow;
            await _repo.UpdateVersionAsync(v);
            return ApiResponse<CourseVersionDto>.SuccessResponse(Map(v), "Version submitted for review");
        }

        public async Task<ApiResponse<CourseVersionDto>> ReviewVersionAsync(int versionId, ReviewCourseVersionDto dto, int reviewerId)
        {
            var v = await _repo.GetVersionAsync(versionId);
            if (v == null) return ApiResponse<CourseVersionDto>.ErrorResponse("Version not found");
            if (v.State != VersionState.InReview)
                return ApiResponse<CourseVersionDto>.ErrorResponse($"Only a version in review can be reviewed (current: {v.State})");

            _context.ContentReviews.Add(new ContentReview
            {
                CourseVersionId = versionId,
                ReviewerId = reviewerId,
                Decision = dto.Decision,
                Summary = dto.Summary,
                CreatedAt = DateTime.UtcNow
            });

            v.State = dto.Decision == ReviewDecision.Approve ? VersionState.Approved : VersionState.Draft;
            _context.CourseVersions.Update(v);
            await _context.SaveChangesAsync();

            var msg = dto.Decision == ReviewDecision.Approve
                ? "Version approved"
                : "Version sent back to Draft";
            return ApiResponse<CourseVersionDto>.SuccessResponse(Map(v), msg);
        }

        public async Task<ApiResponse<CourseVersionDto>> PublishVersionAsync(int versionId, int userId)
        {
            var v = await _repo.GetVersionAsync(versionId);
            if (v == null) return ApiResponse<CourseVersionDto>.ErrorResponse("Version not found");
            if (v.State != VersionState.Approved)
                return ApiResponse<CourseVersionDto>.ErrorResponse($"Only an Approved version can be published (current: {v.State})");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var current = await _repo.GetPublishedVersionAsync(v.CourseId);
            if (current != null && current.CourseVersionId != versionId)
            {
                current.State = VersionState.Archived;
                _context.CourseVersions.Update(current);
                await _context.SaveChangesAsync();
            }

            v.State = VersionState.Published;
            v.PublishedBy = userId;
            v.PublishedAt = DateTime.UtcNow;
            _context.CourseVersions.Update(v);

            var course = await _context.Courses.FirstAsync(c => c.CourseId == v.CourseId);
            if (course.Status == CourseStatus.Draft)
                course.Status = CourseStatus.Published;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return ApiResponse<CourseVersionDto>.SuccessResponse(Map(v), "Version published");
        }

        public async Task<ApiResponse<CourseVersionDto>> ArchiveVersionAsync(int versionId)
        {
            var v = await _repo.GetVersionAsync(versionId);
            if (v == null) return ApiResponse<CourseVersionDto>.ErrorResponse("Version not found");
            if (v.State == VersionState.Published)
                return ApiResponse<CourseVersionDto>.ErrorResponse("Publish another version instead of archiving the live one");

            v.State = VersionState.Archived;
            await _repo.UpdateVersionAsync(v);
            return ApiResponse<CourseVersionDto>.SuccessResponse(Map(v), "Version archived");
        }

        // ================= helpers =================
        private async Task<string?> ValidateCourseRefsAsync(CourseRequestDto dto)
        {
            if (await _catalog.GetSubjectAsync(dto.SubjectId) == null) return "Subject not found";
            if (await _catalog.GetGradeLevelAsync(dto.GradeLevelId) == null) return "Grade level not found";
            if (dto.FrameworkId.HasValue && await _catalog.GetFrameworkAsync(dto.FrameworkId.Value) == null)
                return "Framework not found";
            return null;
        }

        private static CourseDto Map(Course c, bool includeVersions)
        {
            var published = c.Versions?.FirstOrDefault(v => v.State == VersionState.Published);
            return new CourseDto
            {
                CourseId = c.CourseId,
                SubjectId = c.SubjectId,
                SubjectName = c.Subject?.Name,
                GradeLevelId = c.GradeLevelId,
                GradeLevelName = c.GradeLevel?.Name,
                FrameworkId = c.FrameworkId,
                FrameworkName = c.Framework?.Name,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                Status = c.Status,
                ListPrice = c.ListPrice,
                SalePrice = c.SalePrice,
                IsPurchasable = c.IsPurchasable,
                AccessDurationDays = c.AccessDurationDays,
                DisplayOrder = c.DisplayOrder,
                PublishedVersionId = published?.CourseVersionId,
                PublishedVersionNumber = published?.VersionNumber,
                CreatedAt = c.CreatedAt,
                Versions = includeVersions
                    ? c.Versions?.OrderByDescending(v => v.VersionNumber).Select(Map).ToList()
                    : null
            };
        }

        private static CourseVersionDto Map(CourseVersion v) => new()
        {
            CourseVersionId = v.CourseVersionId,
            CourseId = v.CourseId,
            VersionNumber = v.VersionNumber,
            Label = v.Label,
            State = v.State,
            SubmittedBy = v.SubmittedBy,
            SubmittedAt = v.SubmittedAt,
            PublishedBy = v.PublishedBy,
            PublishedAt = v.PublishedAt,
            CreatedAt = v.CreatedAt
        };
    }
}
