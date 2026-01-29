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
                .AsNoTracking()
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null)
                return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null)
                return Unauthorized();

            if (!student.IsProfileCompleted)
                return RedirectToAction("Profile", "Students");

            if (!exam.CollegeId.HasValue ||
                !exam.CourseId.HasValue ||
                !exam.StartDateTime.HasValue ||
                !exam.EndDateTime.HasValue)
            {
                TempData["ErrorMessage"] = "This exam is not assigned yet.";
                return RedirectToAction("Index", "Exams");
            }

            if (exam.CollegeId != student.CollegeId ||
                exam.CourseId != student.CourseId)
            {
                return Unauthorized();
            }

            var now = DateTime.Now;

            if (now < exam.StartDateTime.Value)
            {
                TempData["ErrorMessage"] = "This exam has not started yet.";
                return RedirectToAction("Index", "Exams");
            }

            if (now > exam.EndDateTime.Value)
            {
                TempData["ErrorMessage"] = "This exam has already ended.";
                return RedirectToAction("Index", "Exams");
            }

            var existingAttempt = _context.ExamAttempts
                .AsNoTracking()
                .FirstOrDefault(a =>
                    a.ExamId == examId &&
                    a.StudentId == student.Id
                );

            if (existingAttempt != null)
                return RedirectToAction("AttemptAll", new { attemptId = existingAttempt.Id });

            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = student.Id,
                StartTime = DateTime.Now
            };

            _context.ExamAttempts.Add(attempt);
            _context.SaveChanges();

            return RedirectToAction("AttemptAll", new { attemptId = attempt.Id });
        }

        // -------------------------------
        // ATTEMPT
        /* -------------------------------
        [HttpGet]
        public IActionResult Attempt(int attemptId, int questionIndex = 0)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null)
                return Unauthorized();

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(a => a.StudentAnswers)
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.StudentId != student.Id)
                return Unauthorized();

            if (attempt.EndTime != null)
                return RedirectToAction("Index", "Exams");

            var questions = attempt.Exam.Questions.ToList();

            if (!questions.Any())
                return View("NoQuestions");

            if (questionIndex < 0 || questionIndex >= questions.Count)
                questionIndex = 0;

            var currentQuestion = questions[questionIndex];

            var existingAnswer = attempt.StudentAnswers
                .FirstOrDefault(sa => sa.QuestionId == currentQuestion.Id);

            var model = new ExamAttemptViewModel
            {
                AttemptId = attempt.Id,
                ExamTitle = attempt.Exam.Title,
                DurationInMinutes = attempt.Exam.DurationInMinutes,
                StartTime = attempt.StartTime,
                CurrentQuestionIndex = questionIndex,
                TotalQuestions = questions.Count,
                Question = currentQuestion,
                Questions = questions,
                Options = currentQuestion.Options.ToList(),
                SelectedOptionId = existingAnswer?.SelectedOptionId
            };

            return View(model);
        }
        */
        //Google Form Style

        [HttpGet]
        public IActionResult AttemptAll(int attemptId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(a => a.StudentAnswers)
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.EndTime != null)
                return RedirectToAction("Index", "Exams");

            return View(attempt);
        }


        // -------------------------------
        // SAVE ANSWER
        // -------------------------------
        [HttpPost]
        public IActionResult SaveAnswer(int attemptId, int questionId, int selectedOptionId, int questionIndex)
        {
            var attempt = _context.ExamAttempts.Find(attemptId);

            if (attempt == null || attempt.EndTime != null)
                return Unauthorized();

            var existingAnswer = _context.StudentAnswers
                .FirstOrDefault(sa =>
                    sa.ExamAttemptId == attemptId &&
                    sa.QuestionId == questionId
                );

            if (existingAnswer == null)
            {
                _context.StudentAnswers.Add(new StudentAnswer
                {
                    ExamAttemptId = attemptId,
                    QuestionId = questionId,
                    SelectedOptionId = selectedOptionId,
                    AnsweredAt = DateTime.UtcNow
                });
            }
            else
            {
                existingAnswer.SelectedOptionId = selectedOptionId;
                existingAnswer.AnsweredAt = DateTime.UtcNow;
            }

            _context.SaveChanges();

            return RedirectToAction("Attempt",
                new { attemptId = attemptId, questionIndex = questionIndex });
        }

        // -------------------------------
        // SUBMIT (AUTO-SAVE + MARKS)
        // -------------------------------
        [HttpPost]
        public IActionResult Submit(int attemptId)
        {
            var attempt = _context.ExamAttempts
            .Include(a => a.StudentAnswers)
            .ThenInclude(sa => sa.SelectedOption)
            .Include(a => a.StudentAnswers)
            .ThenInclude(sa => sa.Question)
            .Include(a => a.Exam)
            .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.EndTime != null)
                return RedirectToAction("Index", "Exams");

            /* 🔒 ENSURE ANSWER ROW EXISTS FOR EACH QUESTION
            foreach (var question in attempt.Exam.Questions)
            {
                var answer = attempt.StudentAnswers
                    .FirstOrDefault(sa => sa.QuestionId == question.Id);

                if (answer == null)
                {
                    _context.StudentAnswers.Add(new StudentAnswer
                    {
                        ExamAttemptId = attempt.Id,
                        QuestionId = question.Id,
                        SelectedOptionId = null,
                        AnsweredAt = DateTime.UtcNow
                    });
                }
            }*/

            //_context.SaveChanges();

            // ✅ MARKS-BASED SCORING
            attempt.Score = attempt.StudentAnswers
                .Where(sa => sa.SelectedOption != null && sa.SelectedOption.IsCorrect)
                .Sum(sa => sa.Question.Marks);

            attempt.EndTime = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Index", "Exams");
        }

        //New Submit Method

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitAll(int attemptId, Dictionary<int, int> answers)
        {
            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null || attempt.EndTime != null)
                return Unauthorized();

            // Remove old answers if any (safety)
            var oldAnswers = _context.StudentAnswers
                .Where(sa => sa.ExamAttemptId == attemptId);

            _context.StudentAnswers.RemoveRange(oldAnswers);

            // Save new answers
            foreach (var entry in answers)
            {
                _context.StudentAnswers.Add(new StudentAnswer
                {
                    ExamAttemptId = attemptId,
                    QuestionId = entry.Key,
                    SelectedOptionId = entry.Value,
                    AnsweredAt = DateTime.UtcNow
                });
            }

            _context.SaveChanges();

            // ✅ CORRECT SCORING (EXPLICIT JOIN)
            attempt.Score =
                (from sa in _context.StudentAnswers
                 join opt in _context.Options
                     on sa.SelectedOptionId equals opt.Id
                 join q in _context.Questions
                     on sa.QuestionId equals q.Id
                 where sa.ExamAttemptId == attemptId
                       && opt.IsCorrect
                 select q.Marks
                ).Sum();

            attempt.EndTime = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("Index", "Exams");
        }


        // -------------------------------
        // RESULT
        // -------------------------------
        [HttpGet]
        public IActionResult Result(int attemptId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null)
                return Unauthorized();

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                .AsNoTracking()
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null)
                return NotFound();

            if (attempt.StudentId != student.Id)
                return Unauthorized();

            if (attempt.EndTime == null)
                return RedirectToAction("Index", "Exams");

            if (attempt.Exam.EndDateTime.HasValue &&
                DateTime.Now < attempt.Exam.EndDateTime.Value)
            {
                TempData["ErrorMessage"] = "Results will be available after the exam ends.";
                return RedirectToAction("Index", "Exams");
            }

            return View(attempt);
        }
    }
}
