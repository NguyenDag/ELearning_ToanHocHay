using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Parent;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Helpers;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class ParentLinkService : IParentLinkService
    {
        private readonly AppDbContext _context;
        private readonly IParentLinkRepository _linkRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IDashboardRepository _dashboardRepo;
        private readonly IPackageRepository _packageRepo;

        public ParentLinkService(
            AppDbContext context,
            IParentLinkRepository linkRepo,
            IStudentRepository studentRepo,
            IDashboardRepository dashboardRepo,
            IPackageRepository packageRepo)
        {
            _context = context;
            _linkRepo = linkRepo;
            _studentRepo = studentRepo;
            _dashboardRepo = dashboardRepo;
            _packageRepo = packageRepo;
        }

        public async Task<ApiResponse<ParentInviteDto>> CreateInviteAsync(int parentId, CreateParentInviteDto dto)
        {
            if (!await _context.Parents.AnyAsync(p => p.ParentId == parentId))
                return ApiResponse<ParentInviteDto>.ErrorResponse("Parent not found");

            var invite = new ParentInvite
            {
                ParentId = parentId,
                InviteeEmail = dto.InviteeEmail,
                Token = SecureTokens.NewToken()[..12].ToUpperInvariant(),
                Status = ParentInviteStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(Math.Clamp(dto.ExpiresInDays, 1, 30))
            };
            _context.ParentInvites.Add(invite);
            await _context.SaveChangesAsync();

            return ApiResponse<ParentInviteDto>.SuccessResponse(new ParentInviteDto
            {
                ParentInviteId = invite.ParentInviteId,
                Token = invite.Token,
                InviteeEmail = invite.InviteeEmail,
                Status = invite.Status,
                ExpiresAt = invite.ExpiresAt,
                CreatedAt = invite.CreatedAt
            }, "Mã liên kết đã được tạo");
        }

        public async Task<ApiResponse<ParentLinkDto>> LinkByCodeAsync(int studentId, LinkParentDto dto)
        {
            var code = dto.Code.Trim();
            var now = DateTime.UtcNow;

            int? parentId = null;
            ParentInvite? invite = null;

            // 1. Try a pending, non-expired invite token.
            invite = await _context.ParentInvites
                .FirstOrDefaultAsync(i => i.Token == code && i.Status == ParentInviteStatus.Pending && i.ExpiresAt > now);
            if (invite != null)
                parentId = invite.ParentId;

            // 2. Fall back to a parent's stable ConnectionCode.
            if (parentId == null)
            {
                parentId = await _context.Parents
                    .Where(p => p.ConnectionCode == code)
                    .Select(p => (int?)p.ParentId)
                    .FirstOrDefaultAsync();
            }

            if (parentId == null)
                return ApiResponse<ParentLinkDto>.ErrorResponse("Mã liên kết không hợp lệ hoặc đã hết hạn");

            var student = await _studentRepo.GetStudentWithUserAsync(studentId);
            if (student == null)
                return ApiResponse<ParentLinkDto>.ErrorResponse("Student not found");

            var link = await _linkRepo.GetAsync(parentId.Value, studentId);
            if (link is { Status: LinkStatus.Active })
                return ApiResponse<ParentLinkDto>.ErrorResponse("Đã liên kết với phụ huynh này");

            if (link == null)
            {
                link = new ParentLink
                {
                    ParentId = parentId.Value,
                    StudentId = studentId,
                    Relationship = dto.Relationship,
                    Status = LinkStatus.Active,
                    LinkedAt = now
                };
                await _linkRepo.AddAsync(link);
            }
            else
            {
                link.Status = LinkStatus.Active;
                link.Relationship = dto.Relationship;
                link.LinkedAt = now;
                link.RevokedAt = null;
                await _linkRepo.UpdateAsync(link);
            }

            if (invite != null)
            {
                invite.Status = ParentInviteStatus.Accepted;
                invite.AcceptedAt = now;
                invite.AcceptedByStudentId = studentId;
                await _context.SaveChangesAsync();
            }

            return ApiResponse<ParentLinkDto>.SuccessResponse(
                MapLink(link, student.User?.FullName ?? ""), "Liên kết thành công");
        }

        public async Task<ApiResponse<List<ParentLinkDto>>> GetLinksAsync(int parentId)
        {
            var links = await _linkRepo.GetByParentAsync(parentId);
            return ApiResponse<List<ParentLinkDto>>.SuccessResponse(
                links.Select(l => MapLink(l, l.Student?.User?.FullName ?? "")).ToList());
        }

        public async Task<ApiResponse<bool>> RevokeAsync(int parentId, int studentId)
        {
            var link = await _linkRepo.GetAsync(parentId, studentId);
            if (link == null || link.Status == LinkStatus.Revoked)
                return ApiResponse<bool>.ErrorResponse("Không tìm thấy liên kết đang hoạt động");

            link.Status = LinkStatus.Revoked;
            link.RevokedAt = DateTime.UtcNow;
            await _linkRepo.UpdateAsync(link);
            return ApiResponse<bool>.SuccessResponse(true, "Đã huỷ liên kết");
        }

        public async Task<ApiResponse<List<ChildOverviewDto>>> GetChildrenOverviewAsync(int parentId)
        {
            var links = await _linkRepo.GetByParentAsync(parentId, activeOnly: true);
            var result = new List<ChildOverviewDto>();

            var weekStart = GetWeekStart(DateTime.UtcNow);
            var weekEnd = weekStart.AddDays(7);

            foreach (var link in links)
            {
                var student = await _studentRepo.GetStudentWithUserAsync(link.StudentId);
                if (student == null) continue;

                var week = await _dashboardRepo.GetWeeklyStatsAsync(link.StudentId, weekStart, weekEnd);
                var streak = await _dashboardRepo.GetStreakDataAsync(link.StudentId);
                var sub = await _packageRepo.GetActivePackageAsync(link.StudentId);

                result.Add(new ChildOverviewDto
                {
                    StudentId = student.StudentId,
                    FullName = student.User?.FullName ?? "",
                    GradeLevel = student.CurrentGradeLevelId ?? 0,
                    PackageTier = sub?.Package?.Tier ?? PackageTier.Free,
                    WeeklyStudyMinutes = week.TotalMinutes,
                    WeeklyExercisesCompleted = week.ExerciseCount,
                    WeeklyAverageScore = week.AverageScore,
                    CurrentStreak = streak.CurrentStreak,
                    StudiedToday = streak.StudiedToday
                });
            }

            return ApiResponse<List<ChildOverviewDto>>.SuccessResponse(result);
        }

        private static ParentLinkDto MapLink(ParentLink l, string studentName) => new()
        {
            ParentLinkId = l.ParentLinkId,
            ParentId = l.ParentId,
            StudentId = l.StudentId,
            StudentName = studentName,
            Relationship = l.Relationship,
            Status = l.Status,
            IsPrimaryGuardian = l.IsPrimaryGuardian,
            LinkedAt = l.LinkedAt
        };

        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }
    }
}
