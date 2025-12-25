using Microsoft.AspNetCore.Mvc;

namespace ToanHocHay.WebApp.Controllers
{
    public class ExamController : Controller
    {
        // Trang danh sách chung (nếu cần)
        public IActionResult Index()
        {
            return View();
        }

        // Trang làm bài mặc định
        public IActionResult DoExam()
        {
            ViewData["Title"] = "Đề kiểm tra giữa học kỳ 1 lớp 6";
            return View();
        }

        // --- CÁC ACTION MỚI CHO MENU DROPDOWN ---

        // 1. Đề 15 phút
        public IActionResult Test15Min()
        {
            // Tạm thời dùng chung giao diện DoExam để test
            ViewData["Title"] = "Đề kiểm tra 15 phút - Số học";
            ViewData["Time"] = "15:00";
            return View("DoExam");
        }

        // 2. Đề 1 tiết (45p)
        public IActionResult Test45Min()
        {
            ViewData["Title"] = "Đề kiểm tra 1 tiết - Hình học";
            ViewData["Time"] = "45:00";
            return View("DoExam");
        }

        // 3. Đề Học kỳ
        public IActionResult TestSemester()
        {
            ViewData["Title"] = "Đề thi Học kỳ 1 - Toán 6";
            ViewData["Time"] = "60:00";
            return View("DoExam");
        }

        // 4. Đề HSG
        public IActionResult TestAdvanced()
        {
            ViewData["Title"] = "Đề thi chọn Học sinh giỏi cấp Trường";
            ViewData["Time"] = "90:00";
            return View("DoExam");
        }
        public IActionResult Result()
        {
            return View();
        }
        public IActionResult Review()
        {
            return View();
        }
    }
}