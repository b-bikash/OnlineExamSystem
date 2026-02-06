using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Models.ViewModels;
using System;
using System.Linq;
using System.Collections.Generic;

namespace OnlineExamSystem.Controllers
{
    public class ExamsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------- HELPERS ----------------

        private bool IsTeacherOrAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Teacher" || role == "Admin";
        }

        private bool IsOwnerOrAdmin(Exam exam)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role == "Admin")
                return true;

            if (role == "Teacher" && userId.HasValue && exam.CreatedByTeacherId == userId.Value)
                return true;

            return false;
        }

        // ---------------- INDEX ----------------

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");

            // ================= STUDENT =================
            if (role == "Student")
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                var student = _context.Students
                    .AsNoTracking()
                    .FirstOrDefault(s => s.UserId == userId.Value);

                if (student == null)
                    return Unauthorized();

                var now = DateTime.Now;

                var exams = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                        .ThenInclude(s => s.CourseSubjects)
                    .Include(e => e.CreatedByTeacher)
                    .Where(e =>
                        e.CreatedByTeacher.CollegeId == student.CollegeId &&
                        e.Subject.CourseSubjects.Any(cs => cs.CourseId == student.CourseId) &&
                        e.StartDateTime.HasValue &&
                        e.EndDateTime.HasValue
                    )
                    .OrderByDescending(e => e.EndDateTime)
                    .ToList();

                var attempts = _context.ExamAttempts
                    .AsNoTracking()
                    .Where(a => a.StudentId == student.Id)
                    .ToList();

                var viewModel = new StudentExamsViewModel
                {
                    IsProfileCompleted = student.IsProfileCompleted
                };

                foreach (var exam in exams)
                {
                    var attempt = attempts.FirstOrDefault(a => a.ExamId == exam.Id);

                    if (attempt != null)
                    {
                        var teacherName = _context.Teachers
                            .AsNoTracking()
                            .Where(t => t.UserId == exam.CreatedByTeacherId)
                            .Select(t => t.Name)
                            .FirstOrDefault() ?? "—";

                        viewModel.AttemptedExams.Add(new StudentAttemptedExamItem
                        {
                            Exam = exam,
                            Attempt = attempt,
                            TeacherName = teacherName
                        });
                    }
                    else if (now >= exam.StartDateTime && now <= exam.EndDateTime)
                    {
                        viewModel.LiveExams.Add(exam);
                    }
                    else if (now < exam.StartDateTime)
                    {
                        viewModel.UpcomingExams.Add(exam);
                    }
                }

                viewModel.LiveExams = viewModel.LiveExams
                    .OrderBy(e => e.EndDateTime)
                    .ToList();

                viewModel.UpcomingExams = viewModel.UpcomingExams
                    .OrderBy(e => e.StartDateTime)
                    .ToList();

                viewModel.AttemptedExams = viewModel.AttemptedExams
                    .OrderByDescending(a => a.Attempt.EndTime)
                    .ToList();

                return View(viewModel);
            }

            // ================= TEACHER =================
            if (role == "Teacher")
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return Unauthorized();

                var myExams = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                    .Where(e => e.CreatedByTeacherId == userId.Value)
                    .ToList();

                return View(myExams);
            }

            // ================= ADMIN =================
            if (role != "Admin")
                return Unauthorized();

            var adminExamList = (
                from exam in _context.Exams.AsNoTracking()
                join teacher in _context.Teachers
                    on exam.CreatedByTeacherId equals teacher.UserId into teacherJoin
                from teacher in teacherJoin.DefaultIfEmpty()
                join user in _context.Users
                    on teacher.UserId equals user.Id into userJoin
                from user in userJoin.DefaultIfEmpty()
                select new AdminExamListItemViewModel
                {
                    ExamId = exam.Id,
                    Title = exam.Title,
                    Description = exam.Description,
                    DurationInMinutes = exam.DurationInMinutes,
                    TotalMarks = exam.TotalMarks,
                    TeacherUserId = exam.CreatedByTeacherId,
                    TeacherName = user != null ? user.Username : "—"
                }
            ).ToList();

            return View(adminExamList);
        }

        // ---------------- CREATE ----------------

        [HttpGet]
        public IActionResult Create()
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Exam exam)
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            exam.CreatedByTeacherId = userId.Value;

            _context.Exams.Add(exam);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ---------------- EDIT ----------------

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (exam.StartDateTime.HasValue &&
                exam.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Exam exam)
        {
            if (id != exam.Id)
                return BadRequest();

            var examFromDb = _context.Exams.Find(id);
            if (examFromDb == null)
                return NotFound();

            if (!IsOwnerOrAdmin(examFromDb))
                return Unauthorized();

            if (examFromDb.StartDateTime.HasValue &&
                examFromDb.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            examFromDb.Title = exam.Title;
            examFromDb.Description = exam.Description;
            examFromDb.DurationInMinutes = exam.DurationInMinutes;
            examFromDb.TotalMarks = exam.TotalMarks;
            examFromDb.SubjectId = exam.SubjectId;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- DELETE ----------------

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (exam.StartDateTime.HasValue &&
                exam.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            return View(exam);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (exam.StartDateTime.HasValue &&
                exam.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            _context.Exams.Remove(exam);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- ASSIGN (TIMING ONLY) ----------------

        [HttpGet]
        public IActionResult Assign(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (exam.StartDateTime.HasValue &&
                exam.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be reassigned.";
                return RedirectToAction(nameof(Index));
            }

            bool hasQuestions = _context.Questions.Any(q => q.ExamId == exam.Id);
            if (!hasQuestions)
            {
                TempData["ErrorMessage"] = "You must add at least one question before assigning this exam.";
                return RedirectToAction(nameof(Index));
            }

            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(int id, DateTime startDateTime, DateTime endDateTime)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (exam.StartDateTime.HasValue &&
                exam.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be reassigned.";
                return RedirectToAction(nameof(Index));
            }

            if (endDateTime <= startDateTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            if (!ModelState.IsValid)
                return View(exam);

            exam.StartDateTime = startDateTime;
            exam.EndDateTime = endDateTime;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- RESULTS ----------------

        [HttpGet]
        public IActionResult Results(int examId)
        {
            var exam = _context.Exams
                .AsNoTracking()
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            var attempts = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.ExamId == examId && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .ToList();

            ViewBag.ExamTitle = exam.Title;
            return View(attempts);
        }
    }
}