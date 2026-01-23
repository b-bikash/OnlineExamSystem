using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    public class AdminStudentsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminStudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminStudents + SEARCH (Email / Roll Number)
        public async Task<IActionResult> Index(string search)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var studentsQuery = _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .Include(s => s.College)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                studentsQuery = studentsQuery.Where(s =>
                    (s.User != null && s.User.Email.Contains(search)) ||
                    (s.RollNumber != null && s.RollNumber.Contains(search))
                );
            }


            ViewBag.Search = search;

            var students = await studentsQuery.ToListAsync();
            return View(students);
        }

        // GET: AdminStudents/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Course)
                .Include(s => s.College)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.Courses = _context.Courses.ToList();
            ViewBag.Colleges = _context.Colleges.ToList();

            return View(student);
        }

        // POST: AdminStudents/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Student model)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound();
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
                ViewBag.Courses = _context.Courses.ToList();
                ViewBag.Colleges = _context.Colleges.ToList();
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
