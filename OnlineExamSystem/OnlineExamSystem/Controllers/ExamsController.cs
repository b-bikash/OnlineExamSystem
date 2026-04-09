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
    [ServiceFilter(typeof(SessionValidationFilter))]
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
            return role == "Teacher" || role == "Admin" || role == "TeacherAdmin";
        }

        private bool IsOwnerOrAdmin(Exam exam)
        {
            var role = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetInt32("UserId");
            var teacherId = HttpContext.Session.GetInt32("TeacherId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role == "Admin")
                return true;

            // 🔐 TeacherAdmin → Any exam of their college
            if (role == "TeacherAdmin")
            {
                return sessionCollegeId != null &&
                       exam.CollegeId == sessionCollegeId.Value;
            }

            // 🔐 Teacher → Only their own exam
            if (role == "Teacher")
            {
                if (teacherId.HasValue)
                    return exam.CreatedByTeacherId == teacherId.Value;

                if (userId.HasValue)
                {
                    var teacher = _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefault(t => t.UserId == userId.Value);

                    if (teacher != null)
                        return exam.CreatedByTeacherId == teacher.Id;
                }
            }

            return false;
        }

        // ---------------- HELPERS ----------------

        private void FinalizeAbandonedAttempts(int examId, int? collegeId = null, int? studentId = null)
        {
            var exam = _context.Exams.AsNoTracking().FirstOrDefault(e => e.Id == examId);
            if (exam == null) return;

            var query = _context.ExamAttempts
                .Include(a => a.StudentAnswers)
                .Where(a => a.ExamId == examId && a.EndTime == null);

            if (collegeId.HasValue)
                query = query.Where(a => a.CollegeId == collegeId.Value);

            if (studentId.HasValue)
                query = query.Where(a => a.StudentId == studentId.Value);

            var attempts = query.ToList();

            if (!attempts.Any()) return;

            var now = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime();

            foreach (var attempt in attempts)
            {
                var designatedEndTime = attempt.StartTime.AddMinutes(exam.DurationInMinutes);
                var actualEndTime = exam.EndDateTime.HasValue && exam.EndDateTime.Value < designatedEndTime
                    ? exam.EndDateTime.Value
                    : designatedEndTime;

                // Provide a small grace period (e.g., 2 minutes) before forcibly finalizing
                if (now > actualEndTime.AddMinutes(2))
                {
                    // Calculate score
                    attempt.Score =
                        (from sa in attempt.StudentAnswers
                         join opt in _context.Options on sa.SelectedOptionId equals opt.Id
                         join q in _context.Questions on sa.QuestionId equals q.Id
                         where opt.IsCorrect
                         select q.Marks).Sum();

                    attempt.EndTime = actualEndTime;
                    _context.Update(attempt);
                }
            }

            _context.SaveChanges();
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

                var now = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime();

                var exams = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                        .ThenInclude(s => s.CourseSubjects)
                    .Include(e => e.CreatedByTeacher)
                    .Where(e =>
                        e.CollegeId == student.CollegeId &&
                        e.Subject.CourseSubjects.Any(cs => cs.CourseId == student.CourseId) &&
                        e.StartDateTime.HasValue &&
                        e.EndDateTime.HasValue
                    )
                    .OrderByDescending(e => e.EndDateTime)
                    .ToList();

                // Auto-finalize before loading attempts for the student
                foreach (var exam in exams)
                {
                    FinalizeAbandonedAttempts(exam.Id, student.CollegeId, student.Id);
                }

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
                var collegeId = HttpContext.Session.GetInt32("CollegeId");

                if (userId == null || collegeId == null)
                    return Unauthorized();

                if (teacherId == null)
                {
                    var uniqueTeacher = _context.Teachers
                        .AsNoTracking()
                        .FirstOrDefault(t =>
                            t.UserId == userId.Value &&
                            t.CollegeId == collegeId.Value
                        );

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
                    .Where(e =>
                        e.CreatedByTeacherId == teacherId.Value &&
                        e.CollegeId == collegeId.Value
                    )
                    .ToList();

                return View(exams);
            }

            // ================= ADMIN =================
            if (role == "Admin")
            {
                var query = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                    .Include(e => e.CreatedByTeacher)
                    .Include(e => e.College)
                    .AsQueryable();

                int? collegeId = null;

                if (Request.Query.ContainsKey("collegeId"))
                {
                    if (int.TryParse(Request.Query["collegeId"], out int parsed))
                        collegeId = parsed;
                }

                if (collegeId.HasValue)
                {
                    query = query.Where(e => e.CollegeId == collegeId.Value);
                }

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                ViewBag.SelectedCollegeId = collegeId;

                var exams = query.ToList();

                return View(exams);
            }

            // ================= TEACHERADMIN =================
            if (role == "TeacherAdmin")
            {
                var collegeId = HttpContext.Session.GetInt32("CollegeId");

                if (collegeId == null)
                    return Unauthorized();

                var exams = _context.Exams
                    .AsNoTracking()
                    .Include(e => e.Subject)
                    .Include(e => e.CreatedByTeacher)
                    .Where(e => e.CollegeId == collegeId.Value)
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

            /*if (teacher.CollegeId == null)
            {
                TempData["ErrorMessage"] = "You must be associated with a college to create an exam.";
                return RedirectToAction("Index", "Dashboard");
            }*/

            ViewBag.Subjects = teacher.TeacherSubjects
                .Where(ts => ts.Subject != null)
                .Select(ts => ts.Subject)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name + " (" + s.Code + ")"
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

            // Remove fields that are set programmatically from validation
            ModelState.Remove("CreatedByTeacherId");
            ModelState.Remove("CreatedByTeacher");
            ModelState.Remove("CollegeId");
            ModelState.Remove("College");
            // These form fields are not present in Create, but required by Model
            ModelState.Remove("TotalMarks");
            ModelState.Remove("Subject");
            ModelState.Remove("Questions");
            ModelState.Remove("ExamAttempts");
                       
            if (!ModelState.IsValid)
            {
                ViewBag.Subjects = teacher.TeacherSubjects
                    .Where(ts => ts.Subject != null)
                    .Select(ts => ts.Subject)
                    .OrderBy(s => s.Name)
                    .Select(s => new SelectListItem
                    {
                        Value = s.Id.ToString(),
                        Text = s.Name + " (" + s.Code + ")"
                    })
                    .ToList();

                return View(exam);
            }

            exam.CollegeId = teacher.CollegeId;

            if (role == "Teacher")
            {
                exam.CreatedByTeacherId = teacher.Id;
            }
            else if (role == "TeacherAdmin")
            {
                exam.CreatedByTeacherId = teacher.Id; // optional: or allow selecting teacher later
            }

            exam.CreatedAt = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime();

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
                exam.StartDateTime.Value <= OnlineExamSystem.Helpers.TimeHelper.GetLocalTime())
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
                dbExam.StartDateTime.Value <= OnlineExamSystem.Helpers.TimeHelper.GetLocalTime())
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

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                if (exam.StartDateTime.HasValue &&
                    exam.StartDateTime.Value <= OnlineExamSystem.Helpers.TimeHelper.GetLocalTime())
                {
                    TempData["ErrorMessage"] = "Started exams cannot be deleted by teachers.";
                    return RedirectToAction(nameof(Index));
                }
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

            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                if (exam.StartDateTime.HasValue &&
                    exam.StartDateTime.Value <= OnlineExamSystem.Helpers.TimeHelper.GetLocalTime())
                {
                    TempData["ErrorMessage"] = "Started exams cannot be deleted by teachers.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Manually delete related records if Admin is deleting a Live/Past exam
            var strLogs = _context.ExamBehaviorLogs.Where(l => l.ExamId == id).ToList();
            if(strLogs.Any()) _context.ExamBehaviorLogs.RemoveRange(strLogs);

            var attempts = _context.ExamAttempts
                .Include(a => a.StudentAnswers)
                .Include(a => a.ExamProctorLogs)
                .Where(a => a.ExamId == id).ToList();

            if(attempts.Any()) {
                foreach (var attempt in attempts)
                {
                    if(attempt.StudentAnswers.Any()) _context.StudentAnswers.RemoveRange(attempt.StudentAnswers);
                    if(attempt.ExamProctorLogs.Any()) _context.ExamProctorLogs.RemoveRange(attempt.ExamProctorLogs);
                }
                _context.ExamAttempts.RemoveRange(attempts);
            }

            var questions = _context.Questions
                .Include(q => q.Options)
                .Where(q => q.ExamId == id).ToList();

            if(questions.Any()) {
                foreach(var q in questions) {
                    if(q.Options.Any()) _context.Options.RemoveRange(q.Options);
                }
                _context.Questions.RemoveRange(questions);
            }

            _context.Exams.Remove(exam);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- ASSIGN (SCHEDULE) ----------------

        [HttpGet]
        public IActionResult Assign(int id)
        {
            var exam = _context.Exams
                .Include(e => e.Subject)
                .FirstOrDefault(e => e.Id == id);

            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(int id, DateTime startDateTime, DateTime endDateTime)
        {
            var exam = _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefault(e => e.Id == id);

            if (exam == null)
                return NotFound();

            if (!IsOwnerOrAdmin(exam))
                return Unauthorized();

            if (startDateTime >= endDateTime)
            {
                ModelState.AddModelError("EndDateTime", "End time must be after start time.");
            }
            else if ((endDateTime - startDateTime).TotalMinutes < exam.DurationInMinutes)
            {
                ModelState.AddModelError("EndDateTime", $"The assigned time window ({(endDateTime - startDateTime).TotalMinutes} minutes) must be greater than or equal to the exam duration ({exam.DurationInMinutes} minutes).");
            }

            if (startDateTime < OnlineExamSystem.Helpers.TimeHelper.GetLocalTime())
            {
                ModelState.AddModelError("StartDateTime", "Start time cannot be in the past.");
            }

            if (!exam.Questions.Any())
            {
                ModelState.AddModelError("", "Cannot schedule an exam with no questions. Please add questions first.");
            }

            if (!ModelState.IsValid)
            {
                // Reload subject for display if needed, though mostly read-only in view
                 _context.Entry(exam).Reference(e => e.Subject).Load();
                return View(exam);
            }

            exam.StartDateTime = startDateTime;
            exam.EndDateTime = endDateTime;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Exam scheduled successfully.";
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

            // Auto-finalize before loading attempts
            FinalizeAbandonedAttempts(examId);

            var attempts = _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Student)
                .Include(a => a.ExamProctorLogs) // 📸 For risk calculation
                .Where(a => a.ExamId == examId && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .ToList();

            ViewBag.ExamTitle = exam.Title;
            return View(attempts);
        }
    }
}
