using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role == "Student")
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                if (userId != null)
                {
                    var student = _context.Students
                        .AsNoTracking()
                        .FirstOrDefault(s => s.UserId == userId.Value);

                    ViewBag.Student = student;
                }
            }

            return View();
        }
    }
}
