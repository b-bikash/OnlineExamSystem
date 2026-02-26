using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(AdminAuthorizeFilter))]
    public class AdminStudentsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminStudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminStudents + SEARCH
        public async Task<IActionResult> Index(string search, int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var studentsQuery = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .Include(s => s.College)
                .AsQueryable();

            // 🔐 TeacherAdmin → Only own college
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                studentsQuery = studentsQuery
                    .Where(s => s.CollegeId == sessionCollegeId.Value);
            }
            else if (role == "Admin")
            {
                // 🏫 Optional college filter for Admin
                if (collegeId.HasValue)
                {
                    studentsQuery = studentsQuery
                        .Where(s => s.CollegeId == collegeId.Value);
                }

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                ViewBag.SelectedCollegeId = collegeId;
            }

            // 🔎 Search by Email or Roll Number
            if (!string.IsNullOrWhiteSpace(search))
            {
                studentsQuery = studentsQuery.Where(s =>
                    (s.User != null && s.User.Email.Contains(search)) ||
                    (s.RollNumber != null && s.RollNumber.Contains(search))
                );
            }

            ViewBag.Search = search;

            return View(await studentsQuery.ToListAsync());
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .Include(s => s.College)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return Unauthorized();

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null || student.CollegeId != sessionCollegeId.Value)
                    return Unauthorized();

                ViewBag.Courses = _context.Courses
                    .Where(c => c.CollegeId == sessionCollegeId.Value)
                    .ToList();

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.Id == sessionCollegeId.Value)
                    .ToList();
            }
            else
            {
                ViewBag.Courses = _context.Courses.ToList();
                ViewBag.Colleges = _context.Colleges.ToList();
            }

            return View(student);
        }

        // POST: Edit
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Student model)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return Unauthorized();

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null || student.CollegeId != sessionCollegeId.Value)
                    return Unauthorized();

                // 🔐 Prevent cross-college course assignment
                var validCourse = await _context.Courses.AnyAsync(c =>
                    c.Id == model.CourseId &&
                    c.CollegeId == sessionCollegeId.Value);

                if (!validCourse)
                    return Unauthorized();

                model.CollegeId = sessionCollegeId.Value;
            }

            student.Name = model.Name;
            student.CourseId = model.CourseId;
            student.CollegeId = model.CollegeId;
            student.RollNumber = model.RollNumber;
            student.User.Email = model.User.Email;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Duplicate roll number for the selected college.");

                if (role == "TeacherAdmin")
                {
                    ViewBag.Courses = _context.Courses
                        .Where(c => c.CollegeId == sessionCollegeId.Value)
                        .ToList();

                    ViewBag.Colleges = _context.Colleges
                        .Where(c => c.Id == sessionCollegeId.Value)
                        .ToList();
                }
                else
                {
                    ViewBag.Courses = _context.Courses.ToList();
                    ViewBag.Colleges = _context.Colleges.ToList();
                }

                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}