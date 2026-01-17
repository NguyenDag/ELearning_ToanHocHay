using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.Dtos;
using ToanHocHay.WebApp.Models.ViewModels;
using ToanHocHay.WebApp.Services.Interfaces;

namespace ToanHocHay.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthApiService _authService;

        public AccountController(IAuthApiService authService)
        {
            _authService = authService;
        }
        // 1. Xử lý đường dẫn /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            //ViewBag.Mode = "login"; // Báo cho View hiển thị form đăng nhập
            //return View(); // Tự động tìm file Views/Account/Login.cshtml
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var result = await _authService.LoginAsync(new LoginRequestDto
            {
                Email = vm.Email,
                Password = vm.Password
            });

            if (result == null)
            {
                vm.ErrorMessage = "Email hoặc mật khẩu không đúng";
                return View(vm);
            }

            HttpContext.Session.SetString("AccessToken", result.Token);
            HttpContext.Session.SetString("UserType", result.UserType);
            HttpContext.Session.SetString("FullName", result.FullName);

            return RedirectToAction("Index", "Home");
        }

        /*public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }*/

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