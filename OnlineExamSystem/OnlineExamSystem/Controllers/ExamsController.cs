using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var exams = _context.Exams.ToList();
            return View(exams);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Exam exam)
        {
            _context.Exams.Add(exam);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
