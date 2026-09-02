using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly AppDbContext _context;

        public CatalogRepository(AppDbContext context)
        {
            _context = context;
        }

        // ---------- Subject ----------
        public async Task<List<Subject>> GetSubjectsAsync(bool includeInactive)
        {
            var q = _context.Subjects.AsNoTracking();
            if (!includeInactive) q = q.Where(s => s.IsActive);
            return await q.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync();
        }

        public Task<Subject?> GetSubjectAsync(int subjectId)
            => _context.Subjects.FirstOrDefaultAsync(s => s.SubjectId == subjectId);

        public Task<bool> SubjectCodeExistsAsync(string code, int? exceptId = null)
            => _context.Subjects.AnyAsync(s =>
                s.Code.ToLower() == code.ToLower() && (exceptId == null || s.SubjectId != exceptId));

        public async Task<Subject> AddSubjectAsync(Subject subject)
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return subject;
        }

        public Task UpdateSubjectAsync(Subject subject)
        {
            _context.Subjects.Update(subject);
            return _context.SaveChangesAsync();
        }

        // ---------- GradeLevel ----------
        public async Task<List<GradeLevel>> GetGradeLevelsAsync(bool includeInactive)
        {
            var q = _context.GradeLevels.AsNoTracking();
            if (!includeInactive) q = q.Where(g => g.IsActive);
            return await q.OrderBy(g => g.DisplayOrder).ToListAsync();
        }

        public Task<GradeLevel?> GetGradeLevelAsync(int gradeLevelId)
            => _context.GradeLevels.FirstOrDefaultAsync(g => g.GradeLevelId == gradeLevelId);

        public Task<bool> GradeLevelCodeExistsAsync(string code, int? exceptId = null)
            => _context.GradeLevels.AnyAsync(g =>
                g.Code.ToLower() == code.ToLower() && (exceptId == null || g.GradeLevelId != exceptId));

        public async Task<GradeLevel> AddGradeLevelAsync(GradeLevel gradeLevel)
        {
            _context.GradeLevels.Add(gradeLevel);
            await _context.SaveChangesAsync();
            return gradeLevel;
        }

        public Task UpdateGradeLevelAsync(GradeLevel gradeLevel)
        {
            _context.GradeLevels.Update(gradeLevel);
            return _context.SaveChangesAsync();
        }

        // ---------- CurriculumFramework ----------
        public async Task<List<CurriculumFramework>> GetFrameworksAsync(bool includeInactive)
        {
            var q = _context.CurriculumFrameworks.AsNoTracking();
            if (!includeInactive) q = q.Where(f => f.IsActive);
            return await q.OrderBy(f => f.Name).ToListAsync();
        }

        public Task<CurriculumFramework?> GetFrameworkAsync(int frameworkId)
            => _context.CurriculumFrameworks.FirstOrDefaultAsync(f => f.FrameworkId == frameworkId);

        public Task<bool> FrameworkCodeExistsAsync(string code, int? exceptId = null)
            => _context.CurriculumFrameworks.AnyAsync(f =>
                f.Code.ToLower() == code.ToLower() && (exceptId == null || f.FrameworkId != exceptId));

        public async Task<CurriculumFramework> AddFrameworkAsync(CurriculumFramework framework)
        {
            _context.CurriculumFrameworks.Add(framework);
            await _context.SaveChangesAsync();
            return framework;
        }

        public Task UpdateFrameworkAsync(CurriculumFramework framework)
        {
            _context.CurriculumFrameworks.Update(framework);
            return _context.SaveChangesAsync();
        }
    }
}
