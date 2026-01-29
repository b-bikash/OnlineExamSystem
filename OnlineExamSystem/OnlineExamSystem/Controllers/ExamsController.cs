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

                var assignedExams = _context.Exams
                    .AsNoTracking()
                    .Where(e =>
                        e.CollegeId.HasValue &&
                        e.CourseId.HasValue &&
                        e.StartDateTime.HasValue &&
                        e.EndDateTime.HasValue &&
                        e.CollegeId == student.CollegeId &&
                        e.CourseId == student.CourseId
                    )
                    .ToList();

                var attemptedExamIds = _context.ExamAttempts
                    .AsNoTracking()
                    .Where(a => a.StudentId == student.Id)
                    .Select(a => a.ExamId)
                    .ToList();

                var viewModel = new StudentExamsViewModel();

                foreach (var exam in assignedExams)
                {
                    if (attemptedExamIds.Contains(exam.Id))
                        viewModel.AttemptedExams.Add(exam);
                    else if (now >= exam.StartDateTime && now <= exam.EndDateTime)
                        viewModel.LiveExams.Add(exam);
                    else if (now < exam.StartDateTime)
                        viewModel.UpcomingExams.Add(exam);
                }

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
                    .Where(e => e.CreatedByTeacherId == userId.Value)
                    .ToList();

                return View(myExams);
            }

            // ================= HARD BLOCK =================
            if (role != "Admin")
                return Unauthorized();

            // ================= ADMIN =================
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
        [IgnoreAntiforgeryToken]
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

            examFromDb.Title = exam.Title;
            examFromDb.Description = exam.Description;
            examFromDb.DurationInMinutes = exam.DurationInMinutes;
            examFromDb.TotalMarks = exam.TotalMarks;

            _context.SaveChanges();

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

            _context.Exams.Remove(exam);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ---------------- ASSIGN ----------------

        [HttpGet]
        public IActionResult Assign(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            ViewBag.Colleges = _context.Colleges.AsNoTracking().ToList();
            ViewBag.Courses = _context.Courses.AsNoTracking().ToList();

            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(int id, int collegeId, int courseId, DateTime startDateTime, DateTime endDateTime)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (endDateTime <= startDateTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Colleges = _context.Colleges.AsNoTracking().ToList();
                ViewBag.Courses = _context.Courses.AsNoTracking().ToList();
                return View(exam);
            }

            exam.CollegeId = collegeId;
            exam.CourseId = courseId;
            exam.StartDateTime = startDateTime;
            exam.EndDateTime = endDateTime;

            _context.SaveChanges();

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
