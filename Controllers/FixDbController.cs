/*using ELearning_ToanHocHay_Control.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELearning_ToanHocHay_Control.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FixDbController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FixDbController(AppDbContext context)
        {
            _context = context;
        }

        // ... (Giữ nguyên cái hàm sync-sequence cũ nếu muốn) ...

        // 👇 THÊM HÀM MỚI NÀY VÀO 👇
        [HttpPost("enable-exercise/{id}")]
        public async Task<IActionResult> EnableExercise(int id)
        {
            try
            {
                // SỬA ĐOẠN NÀY: Thêm "IsFree" = true
                await _context.Database.ExecuteSqlRawAsync(
                    $"UPDATE \"Exercise\" SET \"IsActive\" = true, \"Status\" = 1, \"IsFree\" = true WHERE \"ExerciseId\" = {id};");

                return Ok($"Đã mở khóa (Active + Published + Free) cho Đề thi số {id}!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }
        [HttpPost("fix-attempt-sequence-and-get-student/{userId}")]
        public IActionResult FixAttemptAndGetStudent(int userId)
        {
            try
            {
                // 1. Sửa bộ đếm ID cho bảng ExerciseAttempt (để tránh lỗi trùng ID)
                _context.Database.ExecuteSqlRaw(
                    "SELECT setval(pg_get_serial_sequence('\"ExerciseAttempt\"', 'AttemptId'), COALESCE(MAX(\"AttemptId\"), 1) + 1, false) FROM \"ExerciseAttempt\";");

                // 2. Tìm StudentId thật sự dựa trên UserId
                // (Vì bảng Student có ID riêng, không giống User ID)
                var student = _context.Students.FirstOrDefault(s => s.UserId == userId);

                if (student == null)
                {
                    return BadRequest($"Không tìm thấy Học sinh nào gắn với UserId {userId}");
                }

                return Ok(new
                {
                    Message = "Đã sửa lỗi Database thành công!",
                    YourUserId = userId,
                    YourRealStudentId = student.StudentId, // <--- ĐÂY LÀ CÁI BẠN CẦN
                    Note = "Hãy dùng số RealStudentId để bắt đầu làm bài!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }
        [HttpPost("seed-questions/{exerciseId}")]
        public async Task<IActionResult> SeedQuestions(int exerciseId)
        {
            try
            {
                // 1. TÌM USER TỒN TẠI
                var anyUserId = await _context.Database
                    .SqlQueryRaw<int>("SELECT \"UserId\" AS \"Value\" FROM \"User\" LIMIT 1")
                    .FirstOrDefaultAsync();

                if (anyUserId <= 0) return BadRequest("Lỗi: Không tìm thấy User nào trong DB.");

                // 2. TÌM HOẶC TẠO BANK
                int bankId = 0;
                var existingBankId = await _context.Database
                    .SqlQueryRaw<int?>("SELECT MAX(\"BankId\") AS \"Value\" FROM \"QuestionBank\"")
                    .FirstOrDefaultAsync();

                if (existingBankId != null && existingBankId > 0)
                {
                    bankId = existingBankId.Value;
                }
                else
                {
                    await _context.Database.ExecuteSqlRawAsync(@"INSERT INTO ""QuestionBank"" (""BankName"", ""CreatedAt"", ""IsActive"", ""GradeLevel"") VALUES ('Ngân hàng mẫu', NOW(), true, 9);");
                    bankId = await _context.Database.SqlQueryRaw<int>("SELECT MAX(\"BankId\") AS \"Value\" FROM \"QuestionBank\"").FirstOrDefaultAsync();
                }

                // 3. TẠO CÂU HỎI 1
                var sqlInsertQ1 = $@"INSERT INTO ""Question"" (""BankId"", ""QuestionText"", ""QuestionType"", ""Level"", ""DifficultyLevel"", ""TotalScores"", ""CreatedBy"", ""CreatedAt"", ""IsActive"", ""Status"", ""Version"") VALUES ({bankId}, '1 + 1 bằng mấy?', 0, 1, 1, 5, {anyUserId}, NOW(), true, 2, 1);";
                await _context.Database.ExecuteSqlRawAsync(sqlInsertQ1);
                var q1Id = await _context.Database.SqlQueryRaw<int>(@"SELECT MAX(""QuestionId"") AS ""Value"" FROM ""Question""").FirstOrDefaultAsync();
                await _context.Database.ExecuteSqlRawAsync($@"INSERT INTO ""QuestionOption"" (""QuestionId"", ""OptionText"", ""IsCorrect"", ""OrderIndex"") VALUES ({q1Id}, 'Bằng 1', false, 1), ({q1Id}, 'Bằng 2', true, 2), ({q1Id}, 'Bằng 3', false, 3), ({q1Id}, 'Bằng 4', false, 4);");

                // 4. TẠO CÂU HỎI 2
                var sqlInsertQ2 = $@"INSERT INTO ""Question"" (""BankId"", ""QuestionText"", ""QuestionType"", ""Level"", ""DifficultyLevel"", ""TotalScores"", ""CreatedBy"", ""CreatedAt"", ""IsActive"", ""Status"", ""Version"") VALUES ({bankId}, 'Trái đất hình vuông phải không?', 1, 1, 1, 5, {anyUserId}, NOW(), true, 2, 1);";
                await _context.Database.ExecuteSqlRawAsync(sqlInsertQ2);
                var q2Id = await _context.Database.SqlQueryRaw<int>(@"SELECT MAX(""QuestionId"") AS ""Value"" FROM ""Question""").FirstOrDefaultAsync();
                await _context.Database.ExecuteSqlRawAsync($@"INSERT INTO ""QuestionOption"" (""QuestionId"", ""OptionText"", ""IsCorrect"", ""OrderIndex"") VALUES ({q2Id}, 'Đúng', false, 1), ({q2Id}, 'Sai', true, 2);");

                // 5. GẮN VÀO ĐỀ THI
                await _context.Database.ExecuteSqlRawAsync($"DELETE FROM \"ExerciseQuestion\" WHERE \"ExerciseId\" = {exerciseId}");
                await _context.Database.ExecuteSqlRawAsync($@"INSERT INTO ""ExerciseQuestion"" (""ExerciseId"", ""QuestionId"", ""Score"", ""OrderIndex"") VALUES ({exerciseId}, {q1Id}, 5, 1), ({exerciseId}, {q2Id}, 5, 2);");

                // 6. CẬP NHẬT ĐỀ THI (Sửa TotalPoints -> TotalScores)
                // Nếu vẫn lỗi ở dòng này, bạn hãy thử xóa luôn đoạn SET ""TotalScores"" = 10, chỉ giữ lại ""TotalQuestions"" nhé.
                await _context.Database.ExecuteSqlRawAsync($@"
            UPDATE ""Exercise"" 
            SET ""TotalQuestions"" = 2, ""TotalScores"" = 10, ""PassingScore"" = 5
            WHERE ""ExerciseId"" = {exerciseId};
        ");

                return Ok($"Thành công rực rỡ! Đề thi số {exerciseId} đã có 2 câu hỏi mẫu.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }
    }
}*/