using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(AdminAuthorizeFilter))]
    public class AdminTeachersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminTeachersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------- INDEX ----------------
        // GET: AdminTeachers + SEARCH
        public async Task<IActionResult> Index(string search, int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var teachersQuery = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.TeacherSubjects)
                .ThenInclude(ts => ts.Subject)
                .Include(t => t.College)
                .Where(t => t.User != null && t.User.IsActive)
                .AsQueryable();

            // 🔐 TeacherAdmin → Only own college
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                teachersQuery = teachersQuery
                    .Where(t => t.CollegeId == sessionCollegeId.Value);
            }
            else if (role == "Admin")
            {
                if (collegeId.HasValue)
                {
                    teachersQuery = teachersQuery
                        .Where(t => t.CollegeId == collegeId.Value);
                }

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                ViewBag.SelectedCollegeId = collegeId;
            }

            // 🔎 Search by Name or Email
            if (!string.IsNullOrWhiteSpace(search))
            {
                teachersQuery = teachersQuery.Where(t =>
                    (t.Name != null && t.Name.Contains(search)) ||
                    (t.User != null && t.User.Email.Contains(search))
                );
            }

            ViewBag.Search = search;

            return View(await teachersQuery.ToListAsync());
        }

        // ---------------- DETAILS ----------------
        public IActionResult Details(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var teacher = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.TeacherSubjects)
                    .ThenInclude(ts => ts.Subject)
                .FirstOrDefault(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || teacher.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            return View(teacher);
        }

        // ---------------- ASSIGN SUBJECTS (GET) ----------------
        public IActionResult AssignSubjects(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var teacher = _context.Teachers
                .Include(t => t.User)               // ✅ REQUIRED
                .Include(t => t.TeacherSubjects)
                .FirstOrDefault(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            // 🔐 Multi-tenant enforcement
            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || teacher.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            // 🔐 STRICT ROLE CHECK (Corrected)
            if (teacher.User == null || teacher.User.Role != "Teacher")
                return Forbid();

            var subjectsQuery = _context.Subjects.AsQueryable();

            if (role == "TeacherAdmin")
            {
                subjectsQuery = subjectsQuery
                    .Where(s => s.CollegeId == sessionCollegeId.Value);
            }

            var subjects = subjectsQuery.ToList();

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
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var teacher = _context.Teachers
                .Include(t => t.User)   // ✅ REQUIRED
                .FirstOrDefault(t => t.Id == id);

            if (teacher == null)
                return NotFound();

            // 🔐 Multi-tenant enforcement
            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || teacher.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            // 🔐 STRICT ROLE CHECK (via User table)
            if (teacher.User == null || teacher.User.Role != "Teacher")
                return Forbid();

            var existingMappings = _context.TeacherSubjects
                .Where(ts => ts.TeacherId == id);

            _context.TeacherSubjects.RemoveRange(existingMappings);

            if (subjectIds != null && subjectIds.Length > 0)
            {
                foreach (var subjectId in subjectIds)
                {
                    var validSubject = _context.Subjects.Any(s =>
                        s.Id == subjectId &&
                        (role == "Admin" || s.CollegeId == sessionCollegeId));

                    if (!validSubject)
                        continue;

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