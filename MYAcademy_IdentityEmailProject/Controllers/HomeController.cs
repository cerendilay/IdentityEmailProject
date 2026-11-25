using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Models;
using System.Diagnostics;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Ana sayfa açılınca Dashboard'a yönlendir (giriş yapılmadıysa login'e düşer)
            return RedirectToAction("Index", "Dashboard");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
