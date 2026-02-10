using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    public class AdminTeachersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminTeachersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------- INDEX ----------------
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var teachers = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.TeacherSubjects)
                    .ThenInclude(ts => ts.Subject)
                .AsNoTracking()
                .ToList();

            return View(teachers);
        }

        // ---------------- DETAILS (optional) ----------------
        public IActionResult Details(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var teacher = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.TeacherSubjects)
                    .ThenInclude(ts => ts.Subject)
                .FirstOrDefault(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            return View(teacher);
        }

        // ---------------- ASSIGN SUBJECTS (GET) ----------------
        public IActionResult AssignSubjects(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var teacher = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.TeacherSubjects)
                .FirstOrDefault(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            var subjects = _context.Subjects.ToList();

            var assignedSubjectIds = teacher.TeacherSubjects
                .Select(ts => ts.SubjectId)
                .ToList();

            ViewBag.Teacher = teacher;
            ViewBag.Subjects = subjects;
            ViewBag.AssignedSubjectIds = assignedSubjectIds;

            return View();
        }

        // ---------------- ASSIGN SUBJECTS (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignSubjects(int id, int[] subjectIds)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Dashboard");

            // Remove existing mappings
            var existingMappings = _context.TeacherSubjects
                .Where(ts => ts.TeacherId == id);

            _context.TeacherSubjects.RemoveRange(existingMappings);

            // Add new mappings
            if (subjectIds != null && subjectIds.Length > 0)
            {
                foreach (var subjectId in subjectIds)
                {
                    _context.TeacherSubjects.Add(new TeacherSubject
                    {
                        TeacherId = id,
                        SubjectId = subjectId
                    });
                }
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}
