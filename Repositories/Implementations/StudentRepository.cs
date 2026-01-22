using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Repositories.Implementations
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Student> AddAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<Student?> GetByIdAsync(int studentId)
        {
            return await _context.Students
                .FirstOrDefaultAsync(u => u.StudentId == studentId);
        }

        public async Task<Student?> GetByUserIdAsync(int userId)
        {
            return await _context.Students
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }
    }
}
