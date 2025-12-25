using Microsoft.AspNetCore.Mvc;

namespace ToanHocHay.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Trả về view Index.cshtml
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
