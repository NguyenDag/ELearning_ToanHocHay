namespace ELearning_ToanHocHay_Control.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        ICurriculumRepository Curriculums { get; }
        IChapterRepository Chapters { get; }
        ITopicRepository Topics { get; }
        ILessonRepository Lessons { get; }
        ILessonContentRepository LessonContents { get; }

        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> SaveChangesAsync();
    }

}
