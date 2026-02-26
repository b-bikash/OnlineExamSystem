using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Models.ViewModels;

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

                    TotalUsers = _context.Users.Count(),
                    TotalStudents = _context.Students.Count(),
                    TotalTeachers = _context.Teachers.Count(),

                    TotalCourses = _context.Courses.Count(),
                    TotalSubjects = _context.Subjects.Count(),

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

                // Same dashboard view as Admin
                ViewBag.IsTeacherAdmin = true;
                return View();
            }

            return Unauthorized();
        }
    }
}
