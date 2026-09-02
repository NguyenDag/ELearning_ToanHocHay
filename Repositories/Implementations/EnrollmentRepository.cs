using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly AppDbContext _context;

        public EnrollmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task SaveAsync() => _context.SaveChangesAsync();

        public Task<StudentCourse?> GetActiveEnrolmentAsync(int studentId, int courseId)
        {
            var now = DateTime.UtcNow;
            return _context.StudentCourses
                .FirstOrDefaultAsync(sc =>
                    sc.StudentId == studentId &&
                    sc.CourseId == courseId &&
                    sc.Status == StudentCourseStatus.Active &&
                    (sc.AccessExpiresAt == null || sc.AccessExpiresAt > now));
        }

        public async Task<List<StudentCourse>> GetEnrolmentsAsync(int studentId)
            => await _context.StudentCourses
                .AsNoTracking()
                .Include(sc => sc.Course).ThenInclude(c => c!.Subject)
                .Include(sc => sc.Course).ThenInclude(c => c!.GradeLevel)
                .Where(sc => sc.StudentId == studentId)
                .OrderByDescending(sc => sc.EnrolledAt)
                .ToListAsync();

        public async Task<StudentCourse> AddEnrolmentAsync(StudentCourse enrolment)
        {
            _context.StudentCourses.Add(enrolment);
            await _context.SaveChangesAsync();
            return enrolment;
        }

        public async Task<List<PackageEntitlement>> GetActiveEntitlementsAsync(int studentId)
        {
            var now = DateTime.UtcNow;

            var packageIds = await _context.Subscriptions
                .Where(s => s.StudentId == studentId
                            && s.Status == SubscriptionStatus.Active
                            && s.StartDate <= now && s.EndDate > now)
                .Select(s => s.PackageId)
                .ToListAsync();

            if (packageIds.Count == 0) return new List<PackageEntitlement>();

            return await _context.PackageEntitlements
                .AsNoTracking()
                .Where(e => packageIds.Contains(e.PackageId))
                .ToListAsync();
        }
    }
}
