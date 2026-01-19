using Microsoft.AspNetCore.Mvc;

namespace OnlineExamSystem.Controllers
{
    public class DashboardController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
