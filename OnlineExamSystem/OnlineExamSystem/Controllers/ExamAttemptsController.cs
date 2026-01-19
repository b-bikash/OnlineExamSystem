using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    public class ExamAttemptsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ExamAttemptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("ExamAttempts/Start/{examId}")]
        public IActionResult Start(int examId)
        {
            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = 1,
                StartTime = DateTime.Now
            };

            _context.ExamAttempts.Add(attempt);
            _context.SaveChanges();

            return RedirectToAction("Index","Questions", new { examId = examId, attemptId = attempt.Id });
        }


    }
}
