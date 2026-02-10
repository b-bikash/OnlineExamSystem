using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
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
            var teacherId = HttpContext.Session.GetInt32("TeacherId");

            if (role == "Admin")
                return true;

            if (role == "Teacher")
            {
                if (teacherId.HasValue)
                {
                    return exam.CreatedByTeacherId == teacherId.Value;
                }

                // Fallback (e.g. session expired but user still logged in context)
                if (userId.HasValue)
                {
                     var teacher = _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefault(t => t.UserId == userId.Value);
                     if (teacher != null)
                     {
                         return exam.CreatedByTeacherId == teacher.Id;
                     }
                }
            }

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

                var vm = new StudentExamsViewModel
                {
                    IsProfileCompleted = student.IsProfileCompleted
                };

                foreach (var exam in exams)
                {
                    var attempt = attempts.FirstOrDefault(a => a.ExamId == exam.Id);

                    if (attempt != null)
                    {
                        vm.AttemptedExams.Add(new StudentAttemptedExamItem
                        {
                            Exam = exam,
                            Attempt = attempt,
                            TeacherName = exam.CreatedByTeacher?.Name ?? "—"
                        });
                    }
                    else if (now >= exam.StartDateTime && now <= exam.EndDateTime)
                    {
                        vm.LiveExams.Add(exam);
                    }
                    else if (now < exam.StartDateTime)
                    {
                        vm.UpcomingExams.Add(exam);
                    }
                }

                vm.LiveExams = vm.LiveExams.OrderBy(e => e.EndDateTime).ToList();
                vm.UpcomingExams = vm.UpcomingExams.OrderBy(e => e.StartDateTime).ToList();
                vm.AttemptedExams = vm.AttemptedExams
                    .OrderByDescending(a => a.Attempt.EndTime)
                    .ToList();

                return View(vm);
            }

            // ================= TEACHER =================
            if (role == "Teacher")
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var teacherId = HttpContext.Session.GetInt32("TeacherId");

                if (userId == null)
                    return Unauthorized();

                if (teacherId == null)
                {
                     var uniqueTeacher = _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefault(t => t.UserId == userId.Value);
                     if (uniqueTeacher != null)
                     {
                         teacherId = uniqueTeacher.Id;
                         HttpContext.Session.SetInt32("TeacherId", uniqueTeacher.Id);
                     }
                     else
                     {
                         return Unauthorized();
                     }
                }

                var exams = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                    .Where(e => e.CreatedByTeacherId == teacherId.Value)
                    .ToList();

                return View(exams);
            }

            // ================= ADMIN =================
            if (role == "Admin")
            {
                var exams = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                    .Include(e => e.CreatedByTeacher)
                    .ToList();

                return View(exams);
            }

            return Unauthorized();
        }

        // ---------------- CREATE ----------------

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var teacher = _context.Teachers
                .Include(t => t.TeacherSubjects)
                    .ThenInclude(ts => ts.Subject)
                .FirstOrDefault(t => t.UserId == userId.Value);

            if (teacher == null)
                return Unauthorized();

            ViewBag.Subjects = teacher.TeacherSubjects
                .Where(ts => ts.Subject != null)
                .Select(ts => ts.Subject)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            return View(new Exam());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Exam exam)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Teacher")
                return Unauthorized();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            var teacher = _context.Teachers
                .Include(t => t.TeacherSubjects)
                    .ThenInclude(ts => ts.Subject)
                .FirstOrDefault(t => t.UserId == userId.Value);

            if (teacher == null)
                return Unauthorized();

            bool isAllowedSubject = teacher.TeacherSubjects
                .Any(ts => ts.SubjectId == exam.SubjectId);

            if (!isAllowedSubject)
            {
                ModelState.AddModelError(
                    "SubjectId",
                    "You are not allowed to create an exam for this subject."
                );
            }

            // Remove fields that are set programmatically from validation
            ModelState.Remove("CreatedByTeacherId");
            ModelState.Remove("CreatedByTeacher");
            ModelState.Remove("CollegeId");
            ModelState.Remove("College");
            // These form fields are not present in Create, but required by Model
            ModelState.Remove("TotalMarks");
            ModelState.Remove("Subject");
            ModelState.Remove("Questions");

            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = teacher.TeacherSubjects
                    .Where(ts => ts.Subject != null)
                    .Select(ts => ts.Subject)
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name
                    })
                    .ToList();

                return View(exam);
            }

            exam.CreatedByTeacherId = teacher.Id;
            exam.CollegeId = teacher.CollegeId;
            exam.CreatedAt = DateTime.Now;

            _context.Exams.Add(exam);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam created successfully.";
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

            var dbExam = _context.Exams.Find(id);
            if (dbExam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(dbExam))
                return Unauthorized();

            if (dbExam.StartDateTime.HasValue &&
                dbExam.StartDateTime.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "This exam has already started and cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            dbExam.Title = exam.Title;
            dbExam.Description = exam.Description;
            dbExam.DurationInMinutes = exam.DurationInMinutes;
            // dbExam.TotalMarks = exam.TotalMarks; // Do not update marks from edit form (it sends 0)

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- DELETE ----------------

        [HttpPost]
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
                TempData["ErrorMessage"] = "Started exams cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            _context.Exams.Remove(exam);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- RESULTS ----------------

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
