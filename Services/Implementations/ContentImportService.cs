using System.Text.Json;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Content;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ContentImportService : IContentImportService
    {
        private readonly AppDbContext _context;
        private readonly ICourseRepository _courseRepo;
        private readonly IContentRepository _contentRepo;
        private readonly ISystemConfigService _config;

        private const string CfgMaxRows = "content.import.maxRowsPerJob";
        private const string CfgMaxDepth = "content.maxTreeDepth";
        private const int DefaultMaxRows = 5000;
        private const int DefaultMaxDepth = 4;

        public ContentImportService(
            AppDbContext context,
            ICourseRepository courseRepo,
            IContentRepository contentRepo,
            ISystemConfigService config)
        {
            _context = context;
            _courseRepo = courseRepo;
            _contentRepo = contentRepo;
            _config = config;
        }

        // ================================================================
        //  Public entry points
        // ================================================================
        public async Task<ApiResponse<ContentImportResultDto>> ValidateAsync(
            ContentImportSources sources, int? courseVersionId, int userId)
        {
            // Không có versionId nhưng có course.csv ⇒ kiểm tra như luồng "tạo khoá học mới".
            var creatingCourse = courseVersionId is null && sources.Course != null;
            var (result, _, _) = await BuildAsync(
                sources, courseVersionId, creatingCourse, replaceExisting: true);
            result.DryRun = true;
            return ApiResponse<ContentImportResultDto>.SuccessResponse(result,
                result.Valid ? "File hợp lệ để import." : "File có lỗi cần sửa trước khi import.");
        }

        public async Task<ApiResponse<ContentImportResultDto>> ImportIntoVersionAsync(
            ContentImportSources sources, int courseVersionId, bool replaceExisting, bool dryRun, int userId)
        {
            var (result, plan, ctx) = await BuildAsync(sources, courseVersionId, creatingCourse: false, replaceExisting);
            result.DryRun = dryRun;

            if (!result.Valid || dryRun || ctx == null)
            {
                if (!dryRun) await RecordJobAsync(userId, sources, courseVersionId, result, committed: false);
                return Envelope(result);
            }

            await CommitAsync(plan, ctx, replaceExisting, userId, result);
            await RecordJobAsync(userId, sources, result.CourseVersionId, result, committed: true);
            return Envelope(result);
        }

        public async Task<ApiResponse<ContentImportResultDto>> ImportAsNewCourseAsync(
            ContentImportSources sources, bool dryRun, int userId)
        {
            var (result, plan, ctx) = await BuildAsync(sources, courseVersionId: null, creatingCourse: true, replaceExisting: false);
            result.DryRun = dryRun;

            if (!result.Valid || dryRun || ctx == null)
            {
                if (!dryRun) await RecordJobAsync(userId, sources, null, result, committed: false);
                return Envelope(result);
            }

            await CommitAsync(plan, ctx, replaceExisting: false, userId, result);
            await RecordJobAsync(userId, sources, result.CourseVersionId, result, committed: true);
            return Envelope(result);
        }

        // ================================================================
        //  Build + validate (no writes)
        // ================================================================
        private async Task<(ContentImportResultDto Result, ImportPlan Plan, ImportContext? Ctx)> BuildAsync(
            ContentImportSources sources, int? courseVersionId, bool creatingCourse, bool replaceExisting)
        {
            var maxDepth = await _config.GetIntAsync(CfgMaxDepth, DefaultMaxDepth);
            var maxRows = await _config.GetIntAsync(CfgMaxRows, DefaultMaxRows);

            var plan = ContentImportParser.Parse(
                sources.Course, sources.Nodes, sources.Blocks, sources.Flashcards, sources.Resources, maxDepth);

            if (plan.TotalRows > maxRows)
                plan.Add("nodes", null, null, "ROW_CAP_EXCEEDED", ImportIssueSeverity.Error,
                    $"Bộ file có {plan.TotalRows} dòng, vượt giới hạn {maxRows} dòng / lần import.");

            ImportContext? ctx = null;

            // ---- resolve context ----
            if (creatingCourse)
                ctx = await ResolveNewCourseContextAsync(plan);
            else if (courseVersionId is int vid)
            {
                ctx = await ResolveVersionContextAsync(vid, plan);
                if (ctx is { VersionAlreadyHasNodes: true } && !replaceExisting)
                    plan.Add("nodes", null, null, "VERSION_NOT_EMPTY", ImportIssueSeverity.Error,
                        "Version này đã có nội dung. Dùng replace=true để thay toàn bộ, hoặc tạo version Draft mới.");
            }
            else
                plan.Add("nodes", null, null, "NO_TARGET", ImportIssueSeverity.Error,
                    "Chưa xác định đích import: cần courseVersionId hoặc course.csv.");

            // ---- nesting rules (needs subject) ----
            if (ctx != null && !plan.HasErrors)
                await ValidateNestingAsync(plan, ctx.SubjectId);

            var result = ToResult(plan);
            result.CourseId = ctx?.ExistingCourse?.CourseId;
            result.CourseVersionId = ctx?.ExistingVersion?.CourseVersionId;
            return (result, plan, plan.HasErrors ? null : ctx);
        }

        private async Task<ImportContext?> ResolveVersionContextAsync(int versionId, ImportPlan plan)
        {
            var version = await _context.CourseVersions
                .Include(v => v.Course)
                .FirstOrDefaultAsync(v => v.CourseVersionId == versionId);

            if (version == null)
            {
                plan.Add("nodes", null, null, "VERSION_NOT_FOUND", ImportIssueSeverity.Error,
                    $"Không tìm thấy CourseVersion #{versionId}.");
                return null;
            }
            if (version.State != VersionState.Draft)
            {
                plan.Add("nodes", null, null, "VERSION_NOT_DRAFT", ImportIssueSeverity.Error,
                    $"Chỉ import được vào version ở trạng thái Draft (hiện tại: {version.State}).");
                return null;
            }

            if (plan.Course != null)
                plan.Add("course", 1, null, "COURSE_FILE_IGNORED", ImportIssueSeverity.Warning,
                    "Đang import vào version có sẵn nên course.csv bị bỏ qua.");

            var hasNodes = await _context.ContentNodes.AnyAsync(n => n.CourseVersionId == versionId);

            return new ImportContext
            {
                SubjectId = version.Course?.SubjectId,
                ExistingCourse = version.Course,
                ExistingVersion = version,
                VersionAlreadyHasNodes = hasNodes
            };
        }

        private async Task<ImportContext?> ResolveNewCourseContextAsync(ImportPlan plan)
        {
            if (plan.Course == null)
            {
                plan.Add("course", null, null, "MISSING_FILE", ImportIssueSeverity.Error,
                    "Thiếu file course.csv — bắt buộc khi tạo khoá học mới.");
                return null;
            }
            if (plan.HasErrors) return null; // course header already had structural errors

            var h = plan.Course;

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code.ToLower() == h.SubjectCode.ToLower());
            if (subject == null)
                plan.Add("course", 1, null, "SUBJECT_CODE_NOT_FOUND", ImportIssueSeverity.Error,
                    $"Không có môn học với mã '{h.SubjectCode}'. Tạo Subject trước khi import.");

            var grade = await _context.GradeLevels
                .FirstOrDefaultAsync(g => g.Code.ToLower() == h.GradeCode.ToLower());
            if (grade == null)
                plan.Add("course", 1, null, "GRADE_CODE_NOT_FOUND", ImportIssueSeverity.Error,
                    $"Không có khối lớp với mã '{h.GradeCode}'. Tạo GradeLevel trước khi import.");

            CurriculumFramework? framework = null;
            var frameworkWillBeCreated = false;
            if (!string.IsNullOrWhiteSpace(h.FrameworkCode))
            {
                framework = await _context.CurriculumFrameworks
                    .FirstOrDefaultAsync(f => f.Code.ToLower() == h.FrameworkCode.ToLower());
                if (framework == null)
                {
                    frameworkWillBeCreated = true;
                    plan.Add("course", 1, null, "FRAMEWORK_WILL_BE_CREATED", ImportIssueSeverity.Warning,
                        $"Chưa có bộ sách mã '{h.FrameworkCode}' — sẽ tạo mới khi import.");
                }
            }

            if (await _courseRepo.SlugExistsAsync(h.Slug))
                plan.Add("course", 1, null, "COURSE_SLUG_TAKEN", ImportIssueSeverity.Error,
                    $"Slug '{h.Slug}' đã được dùng cho một khoá học khác.");

            if (subject != null && grade != null &&
                await _courseRepo.SubjectGradeFrameworkExistsAsync(subject.SubjectId, grade.GradeLevelId, framework?.FrameworkId))
                plan.Add("course", 1, null, "COURSE_SGF_EXISTS", ImportIssueSeverity.Error,
                    "Đã có khoá học cho đúng tổ hợp Môn × Lớp × Bộ sách này.");

            if (plan.HasErrors) return null;

            return new ImportContext
            {
                SubjectId = subject!.SubjectId,
                NewCourseHeader = h,
                NewCourseSubject = subject,
                NewCourseGrade = grade,
                NewCourseFramework = framework,
                FrameworkWillBeCreated = frameworkWillBeCreated
            };
        }

        private async Task ValidateNestingAsync(ImportPlan plan, int? subjectId)
        {
            var cache = new Dictionary<(NodeType?, NodeType), bool>();

            foreach (var node in plan.Nodes)
            {
                NodeType? parentType = node.ParentKey != null && plan.NodeKeys.TryGetValue(node.ParentKey, out var p)
                    ? p.Type
                    : null;

                var probe = (parentType, node.Type);
                if (!cache.TryGetValue(probe, out var allowed))
                {
                    allowed = await _contentRepo.NodeTypeAllowedAsync(subjectId, parentType, node.Type);
                    cache[probe] = allowed;
                }

                if (!allowed)
                    plan.Add("nodes", node.SourceRow, node.Key, "NESTING_NOT_ALLOWED", ImportIssueSeverity.Error,
                        $"Không cho phép đặt node {node.Type} dưới {(parentType?.ToString() ?? "gốc")} " +
                        "(theo luật NodeTypeRule của môn học).");
            }
        }

        // ================================================================
        //  Commit (writes)
        // ================================================================
        private async Task CommitAsync(
            ImportPlan plan, ImportContext ctx, bool replaceExisting, int userId, ContentImportResultDto result)
        {
            var now = DateTime.UtcNow;
            await using var tx = await _context.Database.BeginTransactionAsync();

            // ---- course + version (new-course path) ----
            CourseVersion version;
            if (ctx.NewCourseHeader != null)
            {
                if (ctx.FrameworkWillBeCreated)
                {
                    ctx.NewCourseFramework = new CurriculumFramework
                    {
                        Code = ctx.NewCourseHeader.FrameworkCode.Trim(),
                        Name = string.IsNullOrWhiteSpace(ctx.NewCourseHeader.FrameworkName)
                            ? ctx.NewCourseHeader.FrameworkCode.Trim()
                            : Truncate(ctx.NewCourseHeader.FrameworkName.Trim(), 150),
                        Publisher = string.IsNullOrWhiteSpace(ctx.NewCourseHeader.Publisher)
                            ? null
                            : Truncate(ctx.NewCourseHeader.Publisher.Trim(), 150),
                        IsActive = true
                    };
                    _context.CurriculumFrameworks.Add(ctx.NewCourseFramework);
                    await _context.SaveChangesAsync();
                }

                var course = new Course
                {
                    SubjectId = ctx.NewCourseSubject!.SubjectId,
                    GradeLevelId = ctx.NewCourseGrade!.GradeLevelId,
                    FrameworkId = ctx.NewCourseFramework?.FrameworkId,
                    Title = Truncate(ctx.NewCourseHeader.Title.Trim(), 255),
                    Slug = ctx.NewCourseHeader.Slug.Trim(),
                    Description = string.IsNullOrWhiteSpace(ctx.NewCourseHeader.Description)
                        ? null : ctx.NewCourseHeader.Description,
                    Status = CourseStatus.Draft,
                    ListPrice = ctx.NewCourseHeader.ListPrice,
                    IsPurchasable = true,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                version = new CourseVersion
                {
                    CourseId = course.CourseId,
                    VersionNumber = 1,
                    Label = string.IsNullOrWhiteSpace(ctx.NewCourseHeader.VersionLabel)
                        ? null : Truncate(ctx.NewCourseHeader.VersionLabel.Trim(), 150),
                    State = VersionState.Draft,
                    CreatedAt = now
                };
                _context.CourseVersions.Add(version);
                await _context.SaveChangesAsync();

                result.CourseId = course.CourseId;
            }
            else
            {
                version = ctx.ExistingVersion!;
                result.CourseId = version.CourseId;

                if (replaceExisting && ctx.VersionAlreadyHasNodes)
                    await ClearVersionAsync(version.CourseVersionId);
            }

            result.CourseVersionId = version.CourseVersionId;

            // ---- nodes (parents first — plan.Nodes is depth-ordered) ----
            foreach (var pn in plan.Nodes)
            {
                var node = new ContentNode
                {
                    CourseVersionId = version.CourseVersionId,
                    ParentNodeId = null,
                    NodeType = pn.Type,
                    Title = pn.Title.Trim(),
                    Slug = pn.Slug,
                    OrderIndex = pn.ResolvedOrderIndex,
                    Depth = pn.Depth,
                    MaterializedPath = "/",
                    IsFree = pn.IsFree,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                _context.ContentNodes.Add(node);
                pn.Entity = node;
            }
            await _context.SaveChangesAsync(); // assign NodeIds

            foreach (var pn in plan.Nodes)
            {
                var node = pn.Entity!;
                pn.PersistedId = node.NodeId;

                ContentNode? parent = pn.ParentKey != null && plan.NodeKeys.TryGetValue(pn.ParentKey, out var pp)
                    ? pp.Entity
                    : null;

                node.ParentNodeId = parent?.NodeId;
                node.MaterializedPath = (parent?.MaterializedPath ?? "/") + node.NodeId + "/";

                if (pn.Type == NodeType.Lesson && pn.DurationMinutes.HasValue)
                    node.LessonDetail = new LessonDetail { NodeId = node.NodeId, DurationMinutes = pn.DurationMinutes };
            }
            await _context.SaveChangesAsync();

            // ---- blocks ----
            var blockOrderByNode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var pb in plan.Blocks)
            {
                var nodeId = plan.NodeKeys[pb.NodeKey].PersistedId;
                var order = pb.OrderIndex ?? NextOrder(blockOrderByNode, pb.NodeKey);

                _context.Add(new ContentBlock
                {
                    NodeId = nodeId,
                    BlockType = pb.BlockType,
                    ContentText = string.IsNullOrEmpty(pb.ContentText) ? null : pb.ContentText,
                    ContentUrl = string.IsNullOrWhiteSpace(pb.ContentUrl) ? null : pb.ContentUrl.Trim(),
                    MetadataJson = string.IsNullOrWhiteSpace(pb.MetadataJson) ? null : pb.MetadataJson,
                    OrderIndex = order
                });
            }

            // ---- flashcard decks + cards ----
            foreach (var pd in plan.Decks)
            {
                var deck = new FlashcardDeck
                {
                    NodeId = plan.NodeKeys[pd.NodeKey].PersistedId,
                    Title = pd.Title.Trim(),
                    CreatedAt = now,
                    Cards = new List<Flashcard>()
                };
                var order = 0;
                foreach (var pc in pd.Cards.OrderBy(c => c.Order == 0 ? int.MaxValue : c.Order).ThenBy(c => c.SourceRow))
                {
                    deck.Cards.Add(new Flashcard
                    {
                        FrontText = pc.Front,
                        BackText = pc.Back,
                        Hint = pc.Hint,
                        OrderIndex = order++
                    });
                }
                _context.FlashcardDecks.Add(deck);
            }

            // ---- resources ----
            var resOrderByNode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var pr in plan.Resources)
            {
                _context.Add(new LessonResource
                {
                    NodeId = plan.NodeKeys[pr.NodeKey].PersistedId,
                    Title = pr.Title.Trim(),
                    ResourceType = pr.ResourceType,
                    ExternalUrl = pr.ExternalUrl.Trim(),
                    IsDownloadable = pr.IsDownloadable,
                    OrderIndex = pr.OrderIndex != 0 ? pr.OrderIndex : NextOrder(resOrderByNode, pr.NodeKey)
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            result.Committed = true;
        }

        private async Task ClearVersionAsync(int versionId)
        {
            var nodeIds = await _context.ContentNodes
                .Where(n => n.CourseVersionId == versionId)
                .Select(n => n.NodeId)
                .ToListAsync();
            if (nodeIds.Count == 0) return;

            var deckIds = await _context.FlashcardDecks
                .Where(d => nodeIds.Contains(d.NodeId)).Select(d => d.DeckId).ToListAsync();

            await _context.Flashcards.Where(c => deckIds.Contains(c.DeckId)).ExecuteDeleteAsync();
            await _context.FlashcardDecks.Where(d => nodeIds.Contains(d.NodeId)).ExecuteDeleteAsync();
            await _context.ContentBlocks.Where(b => nodeIds.Contains(b.NodeId)).ExecuteDeleteAsync();
            await _context.LessonResources.Where(r => nodeIds.Contains(r.NodeId)).ExecuteDeleteAsync();
            await _context.LessonDetails.Where(d => nodeIds.Contains(d.NodeId)).ExecuteDeleteAsync();
            await _context.NodeRevisions.Where(r => nodeIds.Contains(r.NodeId)).ExecuteDeleteAsync();

            // delete deepest nodes first — ParentNodeId FK is Restrict
            var nodes = await _context.ContentNodes
                .Where(n => n.CourseVersionId == versionId)
                .OrderByDescending(n => n.MaterializedPath.Length)
                .ToListAsync();
            _context.ContentNodes.RemoveRange(nodes);
            await _context.SaveChangesAsync();
        }

        // ================================================================
        //  Import job history
        // ================================================================
        private async Task RecordJobAsync(
            int userId, ContentImportSources sources, int? courseVersionId,
            ContentImportResultDto result, bool committed)
        {
            var parts = new List<string>();
            if (sources.Course != null) parts.Add("course.csv");
            if (sources.Nodes != null) parts.Add("nodes.csv");
            if (sources.Blocks != null) parts.Add("blocks.csv");
            if (sources.Flashcards != null) parts.Add("flashcards.csv");
            if (sources.Resources != null) parts.Add("resources.csv");

            var report = JsonSerializer.Serialize(new
            {
                result.Valid,
                result.Committed,
                result.ErrorCount,
                result.WarningCount,
                result.Counts,
                result.Issues
            });

            var job = new ContentImportJob
            {
                UploadedBy = userId,
                FileUrl = Truncate("upload://" + string.Join("+", parts), 1000),
                TargetType = ImportTargetType.ContentNode,
                CourseVersionId = result.CourseVersionId ?? courseVersionId,
                // Commit là toàn-bộ-hoặc-không: đã ghi ⇒ Completed (cảnh báo không phải lỗi dòng).
                Status = committed ? ImportJobStatus.Completed : ImportJobStatus.Failed,
                TotalRows = result.Counts.Chapters + result.Counts.Lessons + result.Counts.OtherNodes
                            + result.Counts.Blocks + result.Counts.Flashcards + result.Counts.Resources,
                SuccessRows = committed
                    ? result.Counts.Chapters + result.Counts.Lessons + result.Counts.OtherNodes
                      + result.Counts.Blocks + result.Counts.Flashcards + result.Counts.Resources
                    : 0,
                ErrorReport = report,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _context.ContentImportJobs.Add(job);
            await _context.SaveChangesAsync();
            result.ImportJobId = job.ImportJobId;
        }

        public async Task<ApiResponse<List<ContentImportJobDto>>> GetJobsAsync(int take)
        {
            take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);
            var jobs = await _context.ContentImportJobs
                .AsNoTracking()
                .Include(j => j.Uploader)
                .OrderByDescending(j => j.CreatedAt)
                .Take(take)
                .ToListAsync();

            return ApiResponse<List<ContentImportJobDto>>.SuccessResponse(jobs.Select(MapJob).ToList());
        }

        public async Task<ApiResponse<ContentImportJobDto>> GetJobAsync(int jobId)
        {
            var job = await _context.ContentImportJobs
                .AsNoTracking()
                .Include(j => j.Uploader)
                .FirstOrDefaultAsync(j => j.ImportJobId == jobId);

            if (job == null) return ApiResponse<ContentImportJobDto>.NotFound("Không tìm thấy phiên import");

            var dto = MapJob(job);
            if (!string.IsNullOrWhiteSpace(job.ErrorReport))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<StoredReport>(job.ErrorReport!);
                    dto.Issues = parsed?.Issues ?? new();
                }
                catch { /* báo cáo cũ / khác định dạng — bỏ qua */ }
            }
            return ApiResponse<ContentImportJobDto>.SuccessResponse(dto);
        }

        private sealed class StoredReport
        {
            public List<ImportIssueDto> Issues { get; set; } = new();
        }

        private static ContentImportJobDto MapJob(ContentImportJob j) => new()
        {
            ImportJobId = j.ImportJobId,
            UploadedBy = j.UploadedBy,
            UploadedByName = j.Uploader?.FullName ?? j.Uploader?.Email,
            FileUrl = j.FileUrl,
            TargetType = j.TargetType.ToString(),
            CourseVersionId = j.CourseVersionId,
            Status = j.Status.ToString(),
            TotalRows = j.TotalRows,
            SuccessRows = j.SuccessRows,
            CreatedAt = j.CreatedAt,
            CompletedAt = j.CompletedAt
        };

        // ================================================================
        //  helpers
        // ================================================================
        private static ContentImportResultDto ToResult(ImportPlan plan)
        {
            var result = new ContentImportResultDto
            {
                Valid = !plan.HasErrors,
                Committed = false,
                ErrorCount = plan.ErrorCount,
                WarningCount = plan.WarningCount,
                Issues = plan.Issues
                    .OrderBy(i => i.Severity)
                    .ThenBy(i => i.File)
                    .ThenBy(i => i.Row ?? 0)
                    .ToList(),
                Counts = new ContentImportCountsDto
                {
                    Chapters = plan.Nodes.Count(n => n.Type == NodeType.Chapter),
                    Lessons = plan.Nodes.Count(n => n.Type == NodeType.Lesson),
                    OtherNodes = plan.Nodes.Count(n => n.Type is NodeType.Topic or NodeType.SubTopic),
                    Blocks = plan.Blocks.Count,
                    FlashcardDecks = plan.Decks.Count,
                    Flashcards = plan.Decks.Sum(d => d.Cards.Count),
                    Resources = plan.Resources.Count
                }
            };
            return result;
        }

        private static ApiResponse<ContentImportResultDto> Envelope(ContentImportResultDto r)
        {
            if (r.Committed)
                return ApiResponse<ContentImportResultDto>.SuccessResponse(r,
                    r.WarningCount > 0
                        ? $"Đã import xong với {r.WarningCount} cảnh báo."
                        : "Đã import thành công.");

            if (r.DryRun)
                return ApiResponse<ContentImportResultDto>.SuccessResponse(r,
                    r.Valid ? "File hợp lệ để import." : "File có lỗi cần sửa trước khi import.");

            return new ApiResponse<ContentImportResultDto>
            {
                Success = false,
                Message = $"Import bị dừng: {r.ErrorCount} lỗi cần sửa trong file.",
                Data = r,
                StatusCode = 400
            };
        }

        private static int NextOrder(Dictionary<string, int> map, string key)
        {
            var v = map.GetValueOrDefault(key);
            map[key] = v + 1;
            return v;
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

        // ================================================================
        //  context
        // ================================================================
        private sealed class ImportContext
        {
            public int? SubjectId { get; set; }

            // existing-version path
            public Course? ExistingCourse { get; set; }
            public CourseVersion? ExistingVersion { get; set; }
            public bool VersionAlreadyHasNodes { get; set; }

            // new-course path
            public CourseHeader? NewCourseHeader { get; set; }
            public Subject? NewCourseSubject { get; set; }
            public GradeLevel? NewCourseGrade { get; set; }
            public CurriculumFramework? NewCourseFramework { get; set; }
            public bool FrameworkWillBeCreated { get; set; }
        }
    }
}
