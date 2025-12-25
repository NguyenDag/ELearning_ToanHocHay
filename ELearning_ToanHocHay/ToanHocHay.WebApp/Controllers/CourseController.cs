using Microsoft.AspNetCore.Mvc;

namespace ToanHocHay.WebApp.Controllers
{
    public class CourseController : Controller
    {
        // Trang danh sách khóa học (Nếu cần)
        public IActionResult Index()
        {
            return View();
        }

        // Trang học bài chi tiết (Learning Player)
        // URL: /Course/Lesson
        public IActionResult Lesson()
        {
            // Trong thực tế, bạn sẽ nhận id bài học ở đây: public IActionResult Lesson(int id)
            // Và lấy dữ liệu từ Database. Hiện tại trả về View tĩnh.
            return View();
        }
    }
}