using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            Curriculums = new CurriculumRepository(_context);
            Chapters = new ChapterRepository(_context);
            Topics = new TopicRepository(_context);
            Lessons = new LessonRepository(_context);
            LessonContents = new LessonContentRepository(_context);
            Users = new UserRepository(_context);
        }

        public IUserRepository Users { get; private set; }
        public ICurriculumRepository Curriculums { get; private set; }
        public IChapterRepository Chapters { get; private set; }
        public ITopicRepository Topics { get; private set; }
        public ILessonRepository Lessons { get; private set; }
        public ILessonContentRepository LessonContents { get; private set; }


        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync(); 
                _transaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }

}
