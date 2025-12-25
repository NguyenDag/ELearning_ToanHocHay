using Microsoft.AspNetCore.Mvc;

namespace ToanHocHay.WebApp.Controllers
{
    public class AccountController : Controller
    {
        // 1. Xử lý đường dẫn /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.Mode = "login"; // Báo cho View hiển thị form đăng nhập
            return View(); // Tự động tìm file Views/Account/Login.cshtml
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Logic kiểm tra đăng nhập sẽ viết ở đây
            // Tạm thời chuyển hướng về trang chủ
            return RedirectToAction("Index", "Home");
        }

        // 2. Xử lý đường dẫn /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Mode = "register"; // Báo cho View hiển thị form đăng ký
            return View("Login"); // Tái sử dụng file Login.cshtml
        }

        [HttpPost]
        public IActionResult Register(string fullName, string email, string password, string role)
        {
            // Logic tạo tài khoản sẽ viết ở đây
            return RedirectToAction("Index", "Home");
        }

        // 3. Xử lý đường dẫn /Account/Logout
        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}