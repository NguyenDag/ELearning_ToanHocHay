using AutoMapper;
using ELearning_ToanHocHay_Control.Models.DTOs.Chapter;
using ELearning_ToanHocHay_Control.Models.DTOs.Student;
using ELearning_ToanHocHay_Control.Models.DTOs.Student.Dashboard;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using SendGrid.Helpers.Errors.Model;

namespace ELearning_ToanHocHay_Control.Services.Implementations
{
    public class CoreDashboardService : ICoreDashboardService
    {

        private readonly IDashboardRepository _dashboardRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IPackageRepository _packageRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<CoreDashboardService> _logger;

        public CoreDashboardService(
            IDashboardRepository dashboardRepo,
            IStudentRepository studentRepo,
            IPackageRepository packageRepo,
            IMapper mapper,
            ILogger<CoreDashboardService> logger)
        {
            _dashboardRepo = dashboardRepo;
            _studentRepo = studentRepo;
            _packageRepo = packageRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<CoreDashboardDto> GetCoreDashboardAsync(int studentId)
        {
            // Check cache first
            var cacheKey = $"dashboard:core:{studentId}";

            _logger.LogInformation("Building core dashboard for student {StudentId}", studentId);

            // Execute queries in parallel for performance
            var studentInfoTask = await GetStudentInfoAsync(studentId);
            var statsTask = await GetOverviewStatsAsync(studentId);
            var recentLessonsTask = await GetRecentLessonsAsync(studentId, 5);
            var chapterProgressTask = await GetChapterProgressSummaryAsync(studentId);
            var packageTypeTask = await GetPackageTypeAsync(studentId);


            var packageType = packageTypeTask;

            var dashboard = new CoreDashboardDto
            {
                StudentInfo =  studentInfoTask,
                Stats =  statsTask,
                RecentLessons = recentLessonsTask,
                ChapterProgress = chapterProgressTask,
                PackageType = packageType,
                Links = GenerateDashboardLinks(studentId, packageType)
            };

            return dashboard;
        }

        private async Task<StudentInfoDto> GetStudentInfoAsync(int studentId)
        {
            var student = await _studentRepo.GetStudentWithUserAsync(studentId);

            if (student == null)
                throw new NotFoundException($"Student {studentId} not found");

            return new StudentInfoDto
            {
                StudentId = student.StudentId,
                FullName = student.User.FullName,
                GradeLevel = student.GradeLevel,
                SchoolName = student.SchoolName,
            };
        }

        private async Task<OverviewStatsDto> GetOverviewStatsAsync(int studentId)
        {
            // Lấy stats tuần này
            var weekStart = GetWeekStart(DateTime.UtcNow);
            var weekEnd = weekStart.AddDays(7);

            var thisWeekStats = await _dashboardRepo.GetWeeklyStatsAsync(
                studentId, weekStart, weekEnd);

            // Lấy stats tuần trước để so sánh
            var lastWeekStart = weekStart.AddDays(-7);
            var lastWeekStats = await _dashboardRepo.GetWeeklyStatsAsync(
                studentId, lastWeekStart, weekStart);

            // Lấy thống kê tổng thể
            var overallStats = await _dashboardRepo.GetOverallStatsAsync(studentId);

            // Tính streak
            var streakData = await _dashboardRepo.GetStreakDataAsync(studentId);

            // So sánh tuần này vs tuần trước
            var comparison = new ComparisonDto
            {
                ScoreChange = (int)(thisWeekStats.AverageScore - lastWeekStats.AverageScore),
                StudyTimeChange = thisWeekStats.TotalMinutes - lastWeekStats.TotalMinutes,
                ExerciseCountChange = thisWeekStats.ExerciseCount - lastWeekStats.ExerciseCount,
                Direction = DetermineDirection(
                    thisWeekStats.AverageScore,
                    lastWeekStats.AverageScore)
            };

            return new OverviewStatsDto
            {
                WeeklyStudyMinutes = thisWeekStats.TotalMinutes,
                WeeklyExercisesCompleted = thisWeekStats.ExerciseCount,
                AverageScore = overallStats.AverageScore,
                TotalExercisesCompleted = overallStats.TotalExercises,
                TotalLessonsCompleted = overallStats.TotalLessons,
                WeekComparison = comparison,
                CurrentStreak = streakData.CurrentStreak,
                LongestStreak = streakData.LongestStreak,
                StudiedToday = streakData.StudiedToday
            };
        }

        private async Task<List<RecentLessonDto>> GetRecentLessonsAsync(int studentId, int limit)
        {
            var recentLessons = await _dashboardRepo.GetRecentLessonsAsync(studentId, limit);

            return recentLessons.Select(l => new RecentLessonDto
            {
                LessonId = l.LessonId,
                LessonName = l.LessonName,
                TopicName = l.TopicName,
                ChapterName = l.ChapterName,
                CompletedAt = l.CompletedAt,
                DurationMinutes = l.DurationMinutes,
                IsCompleted = l.IsCompleted,
                ProgressPercentage = l.ProgressPercentage
            }).ToList();
        }

        private async Task<List<ChapterProgressSummaryDto>> GetChapterProgressSummaryAsync(int studentId)
        {
            var chapterProgress = await _dashboardRepo.GetChapterProgressAsync(studentId);

            return chapterProgress.Select(cp => new ChapterProgressSummaryDto
            {
                ChapterId = cp.ChapterId,
                ChapterName = cp.ChapterName,
                OrderIndex = cp.OrderIndex,
                CompletionPercentage = cp.CompletionPercentage,
                CompletedTopics = cp.CompletedTopics,
                TotalTopics = cp.TotalTopics,
                IsLocked = cp.IsLocked,
                CurrentMastery = cp.AverageMastery
            })
            .OrderBy(cp => cp.OrderIndex)
            .ToList();
        }

        private async Task<PackageType> GetPackageTypeAsync(int studentId)
        {
            var activePackage = await _packageRepo.GetActivePackageAsync(studentId);
            //return activePackage?.PackageType ?? PackageType.Free;
            return PackageType.Free;
        }

        private DashboardLinksDto GenerateDashboardLinks(int studentId, PackageType packageType)
        {
            var baseUrl = $"/api/student/{studentId}/dashboard";

            return new DashboardLinksDto
            {
                ExerciseHistory = $"{baseUrl}/exercise-history",
                Charts = packageType >= PackageType.Standard
                    ? $"{baseUrl}/charts"
                    : null,
                AIInsights = packageType >= PackageType.Premium
                    ? $"{baseUrl}/ai-insights"
                    : null,
                Notifications = packageType >= PackageType.Standard
                    ? $"{baseUrl}/notifications"
                    : null
            };
        }

        public async Task<bool> VerifyStudentAccessAsync(int studentId, int userId)
        {
            var student = await _studentRepo.GetByIdAsync(studentId);
            return student?.UserId == userId;
        }

        private DateTime GetWeekStart(DateTime date)
        {
            // Start from Monday
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        private TrendDirection DetermineDirection(decimal current, decimal previous)
        {
            var diff = current - previous;
            if (Math.Abs(diff) < 1) return TrendDirection.Same;
            return diff > 0 ? TrendDirection.Up : TrendDirection.Down;
        }
    }

    public enum PackageType
    {
        Free = 0,
        Standard = 1,
        Premium = 2,
        Yearly = 3
    }
}
