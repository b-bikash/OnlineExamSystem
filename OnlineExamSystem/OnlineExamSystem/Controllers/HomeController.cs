using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    // Public controller (NO authentication)
    public class HomeController : Controller
    {
        // Public landing page
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
