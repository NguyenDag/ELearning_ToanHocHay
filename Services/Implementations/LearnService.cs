using System.Security.Claims;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class LearnService : ILearnService
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IContentRepository _contentRepo;
        private readonly IContentAccessService _access;

        public LearnService(
            ICourseRepository courseRepo, IContentRepository contentRepo, IContentAccessService access)
        {
            _courseRepo = courseRepo;
            _contentRepo = contentRepo;
            _access = access;
        }

        public async Task<ApiResponse<CourseContentDto>> GetCourseContentAsync(ClaimsPrincipal user, int courseId)
        {
            var course = await _courseRepo.GetCourseAsync(courseId, withVersions: true);
            if (course == null) return ApiResponse<CourseContentDto>.ErrorResponse("Course not found");

            var level = await _access.GetCourseAccessAsync(user, course);
            if (level == ContentAccessLevel.None)
                return ApiResponse<CourseContentDto>.ErrorResponse("Course not found");

            var version = course.Versions?.FirstOrDefault(v => v.State == VersionState.Published)
                          ?? course.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            if (version == null)
                return ApiResponse<CourseContentDto>.ErrorResponse("Course has no content yet");

            var nodes = await _contentRepo.GetNodesByVersionAsync(version.CourseVersionId);

            if (level == ContentAccessLevel.FreeOnly)
                nodes = FilterToFree(nodes);

            var dto = new CourseContentDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Slug = course.Slug,
                Description = course.Description,
                CourseVersionId = version.CourseVersionId,
                VersionNumber = version.VersionNumber,
                AccessLevel = level.ToString(),
                IsEntitled = level == ContentAccessLevel.Full,
                Tree = ContentMapping.BuildTree(nodes, includeHidden: false)
            };
            return ApiResponse<CourseContentDto>.SuccessResponse(dto);
        }

        public async Task<ApiResponse<ContentNodeDetailDto>> GetNodeAsync(ClaimsPrincipal user, int nodeId)
        {
            var node = await _contentRepo.GetNodeForConsumptionAsync(nodeId);
            if (node?.CourseVersion?.Course == null)
                return ApiResponse<ContentNodeDetailDto>.ErrorResponse("Node not found");

            if (node.IsHidden)
                return ApiResponse<ContentNodeDetailDto>.ErrorResponse("Node not found");

            var course = node.CourseVersion.Course;
            var level = await _access.GetCourseAccessAsync(user, course);

            if (level == ContentAccessLevel.None)
                return ApiResponse<ContentNodeDetailDto>.ErrorResponse("Node not found");

            // A published course must be consumed through its published version.
            if (course.Status == CourseStatus.Published && node.CourseVersion.State != VersionState.Published)
                return ApiResponse<ContentNodeDetailDto>.ErrorResponse("Node not found");

            if (level == ContentAccessLevel.FreeOnly && !node.IsFree)
                return ApiResponse<ContentNodeDetailDto>.Forbidden(
                    "Bài học này cần gói đang hoạt động hoặc đã ghi danh");

            return ApiResponse<ContentNodeDetailDto>.SuccessResponse(ContentMapping.MapNodeDetail(node));
        }

        /// <summary>Keeps free nodes and every ancestor of a free node so the tree stays navigable.</summary>
        private static List<ContentNode> FilterToFree(List<ContentNode> nodes)
        {
            var keep = new HashSet<int>();
            foreach (var n in nodes.Where(n => n.IsFree && !n.IsHidden))
            {
                keep.Add(n.NodeId);
                foreach (var idStr in n.MaterializedPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(idStr, out var id)) keep.Add(id);
            }
            return nodes.Where(n => keep.Contains(n.NodeId)).ToList();
        }
    }
}
