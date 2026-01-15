using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;


namespace OnlineExamSystem.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var questions = _context.Questions
        .Include(q => q.Exam)
        .ToList();

            return View(questions);
        }

        public IActionResult Create()
        {
            ViewBag.Exams = _context.Exams.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Question question)
        {
            _context.Questions.Add(question);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }


    }
}
