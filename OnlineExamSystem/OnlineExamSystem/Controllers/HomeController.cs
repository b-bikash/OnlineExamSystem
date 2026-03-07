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

        [HttpPost("/test-import")]
        public async Task<IActionResult> TestImport([FromServices] OnlineExamSystem.Services.ImportExport.IImportService importService, IFormFile file, int collegeId = 1)
        {
            try 
            {
                var result = await importService.ImportCourseSubjectAsync(file, collegeId);
                return Content("Success! " + System.Text.Json.JsonSerializer.Serialize(result));
            }
            catch(Exception ex)
            {
                return Content("Exception! " + ex.ToString());
            }
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
