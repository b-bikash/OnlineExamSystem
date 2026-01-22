using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    public class ExamAttemptsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ExamAttemptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------
        // START EXAM
        // -------------------------------
        [HttpGet]
        public IActionResult Start(int examId)
        {
            var exam = _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null)
                return NotFound();

            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = 1, // TEMP (replace later with logged-in student)
                StartTime = DateTime.Now
            };

            _context.ExamAttempts.Add(attempt);
            _context.SaveChanges();

            return RedirectToAction("Attempt", new { attemptId = attempt.Id });
        }

        // -------------------------------
        // ATTEMPT UI PAGE
        // -------------------------------
        [HttpGet]
        public IActionResult Attempt(int attemptId, int questionIndex = 0)
        {
            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                        //.ThenInclude(q => q.Answers)
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound();

            var questions = attempt.Exam.Questions.ToList();

            if (!questions.Any())
                return View("NoQuestions");

            if (questionIndex < 0 || questionIndex >= questions.Count)
                questionIndex = 0;

            var model = new ExamAttemptViewModel
            {
                AttemptId = attempt.Id,
                ExamTitle = attempt.Exam.Title,
                DurationInMinutes = attempt.Exam.DurationInMinutes,
                StartTime = attempt.StartTime,
                CurrentQuestionIndex = questionIndex,
                TotalQuestions = questions.Count,
                Question = questions[questionIndex],
                Questions = questions
            };

            return View(model);
        }

        // -------------------------------
        // SUBMIT EXAM (placeholder)
        // -------------------------------
        [HttpPost]
        public IActionResult Submit(int attemptId)
        {
            var attempt = _context.ExamAttempts.Find(attemptId);

            if (attempt == null)
                return NotFound();

            attempt.EndTime = DateTime.Now;
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
        }
    }
}
