using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Models.ViewModels;
using OnlineExamSystem.ViewModels;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(SessionValidationFilter))]
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

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            // ================= STUDENT =================
            if (role == "Student")
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var collegeId = HttpContext.Session.GetInt32("CollegeId");

                if (userId != null && collegeId != null)
                {
                    var student = _context.Students
                        .AsNoTracking()
                        .FirstOrDefault(s =>
                            s.UserId == userId.Value &&
                            s.CollegeId == collegeId.Value
                        );

                    ViewBag.Student = student;
                }

                return View();
            }

            // ================= TEACHER =================
            if (role == "Teacher")
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var collegeId = HttpContext.Session.GetInt32("CollegeId");

                if (userId != null && collegeId != null)
                {
                    var teacher = _context.Teachers
                        .Include(t => t.TeacherSubjects)
                            .ThenInclude(ts => ts.Subject)
                        .AsNoTracking()
                        .FirstOrDefault(t =>
                            t.UserId == userId.Value &&
                            t.CollegeId == collegeId.Value
                        );

                    return View(teacher);
                }

                return View();
            }

            // ================= ADMIN =================
            if (role == "Admin")
            {
                var now = DateTime.Now;

                var vm = new AdminDashboardViewModel
                {
                    TotalColleges = _context.Colleges.Count(),
                    ActiveColleges = _context.Colleges.Count(c => c.IsActive),

                    TotalUsers = _context.Users.Count(c => c.IsActive),
                    TotalStudents = _context.Students.Count(),
                    TotalTeachers = _context.Teachers.Count(),

                    TotalCourses = _context.Courses.Count(c => c.IsActive),
                    TotalSubjects = _context.Subjects.Count(c => c.IsActive),

                    TotalExams = _context.Exams.Count(),
                    LiveExams = _context.Exams.Count(e =>
                        e.StartDateTime.HasValue &&
                        e.EndDateTime.HasValue &&
                        e.StartDateTime <= now &&
                        e.EndDateTime >= now),

                    UpcomingExams = _context.Exams.Count(e =>
                        e.StartDateTime.HasValue &&
                        e.StartDateTime > now),

                    PastExams = _context.Exams.Count(e =>
                        e.EndDateTime.HasValue &&
                        e.EndDateTime < now)
                };

                return View(vm);
            }

            // ================= TEACHER ADMIN =================
            if (role == "TeacherAdmin")
            {
                var collegeId = HttpContext.Session.GetInt32("CollegeId");

                if (collegeId == null)
                    return Unauthorized();

                var now = DateTime.UtcNow;

                var model = new TeacherAdminDashboardViewModel
                {
                    TotalStudents = _context.Students
                        .AsNoTracking()
                        .Count(s => s.CollegeId == collegeId),

                    TotalTeachers = _context.Teachers
                        .AsNoTracking()
                        .Count(t => t.CollegeId == collegeId),

                    TotalCourses = _context.Courses
                        .AsNoTracking()
                        .Count(c => c.CollegeId == collegeId),

                    TotalSubjects = _context.Subjects
                        .AsNoTracking()
                        .Count(s => s.CollegeId == collegeId),

                    TotalExams = _context.Exams
                        .AsNoTracking()
                        .Count(e => e.CollegeId == collegeId),

                    LiveExamsCount = _context.Exams
                        .AsNoTracking()
                        .Count(e =>
                            e.CollegeId == collegeId &&
                            e.StartDateTime <= now &&
                            e.EndDateTime >= now),

                    UpcomingExamsCount = _context.Exams
                        .AsNoTracking()
                        .Count(e =>
                            e.CollegeId == collegeId &&
                            e.StartDateTime > now),

                    CompletedExamsCount = _context.Exams
                        .AsNoTracking()
                        .Count(e =>
                            e.CollegeId == collegeId &&
                            e.EndDateTime < now),

                    TotalExamAttempts = _context.ExamAttempts
                        .AsNoTracking()
                        .Count(a => a.CollegeId == collegeId),

                    AverageScore = _context.ExamAttempts
                        .AsNoTracking()
                        .Where(a => a.CollegeId == collegeId)
                        .Select(a => (double?)a.Score)
                        .Average()
                };

                var examStats = _context.Exams
                .AsNoTracking()
                .Where(e => e.CollegeId == collegeId)
                .GroupBy(e => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Live = g.Count(e => e.StartDateTime <= now && e.EndDateTime >= now),
                    Upcoming = g.Count(e => e.StartDateTime > now),
                    Completed = g.Count(e => e.EndDateTime < now)
                })
                .FirstOrDefault();

                model.TotalExams = examStats?.Total ?? 0;
                model.LiveExamsCount = examStats?.Live ?? 0;
                model.UpcomingExamsCount = examStats?.Upcoming ?? 0;
                model.CompletedExamsCount = examStats?.Completed ?? 0; 
                
                ViewBag.IsTeacherAdmin = true;
                return View(model);
            }

            return Unauthorized();
        }
    }
}
