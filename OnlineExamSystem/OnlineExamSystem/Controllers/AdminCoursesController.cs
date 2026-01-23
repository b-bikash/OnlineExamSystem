using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    public class AdminCoursesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminCoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminCourses + SEARCH
        public IActionResult Index(string search)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var coursesQuery = _context.Courses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                coursesQuery = coursesQuery.Where(c =>
                    c.Name != null && c.Name.Contains(search)
                );
            }

            ViewBag.Search = search;

            var courses = coursesQuery.ToList();
            return View(courses);
        }

        // GET: AdminCourses/Create
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // POST: AdminCourses/Create
        [HttpPost]
        public IActionResult Create(Course model)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Course name is required.");
                return View(model);
            }

            var courseExists = _context.Courses
                .Any(c => c.Name.ToLower() == model.Name.ToLower());

            if (courseExists)
            {
                ModelState.AddModelError("Name", "A course with this name already exists.");
                return View(model);
            }

            _context.Courses.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Course added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminCourses/Edit/5
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var course = _context.Courses.FirstOrDefault(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: AdminCourses/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, Course model)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var course = _context.Courses.FirstOrDefault(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Course name is required.");
                return View(course);
            }

            var duplicateExists = _context.Courses
                .Any(c => c.Id != id && c.Name.ToLower() == model.Name.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A course with this name already exists.");
                return View(course);
            }

            course.Name = model.Name;
            course.IsApproved = model.IsApproved;

            _context.SaveChanges();

            TempData["Success"] = "Course updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminCourses/Delete/5
        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var course = _context.Courses.FirstOrDefault(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            _context.SaveChanges();

            TempData["Success"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
