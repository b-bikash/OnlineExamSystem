using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Models.ViewModels;

namespace OnlineExamSystem.Controllers
{
    public class AdminCoursesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminCoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------
        // INDEX + SEARCH
        // -------------------------------
        public IActionResult Index(string search)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var coursesQuery = _context.Courses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                coursesQuery = coursesQuery.Where(c =>
                    c.Name != null && c.Name.Contains(search));
            }

            ViewBag.Search = search;
            return View(coursesQuery.ToList());
        }

        // -------------------------------
        // CREATE
        // -------------------------------
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpPost]
        public IActionResult Create(Course model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Course name is required.");
                return View(model);
            }

            bool exists = _context.Courses
                .Any(c => c.Name.ToLower() == model.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "A course with this name already exists.");
                return View(model);
            }

            _context.Courses.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Course added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------
        // EDIT
        // -------------------------------
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.Find(id);
            if (course == null)
                return NotFound();

            return View(course);
        }

        [HttpPost]
        public IActionResult Edit(int id, Course model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.Find(id);
            if (course == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Course name is required.");
                return View(course);
            }

            bool duplicate = _context.Courses
                .Any(c => c.Id != id && c.Name.ToLower() == model.Name.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError("Name", "A course with this name already exists.");
                return View(course);
            }

            course.Name = model.Name;
            _context.SaveChanges();

            TempData["Success"] = "Course updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------
        // DELETE
        // -------------------------------
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.Find(id);
            if (course == null)
                return NotFound();

            _context.Courses.Remove(course);
            _context.SaveChanges();

            TempData["Success"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // PHASE 2.1 — COURSE ⇄ SUBJECT MANAGEMENT
        // =====================================================

        // -------------------------------
        // VIEW ASSIGNED SUBJECTS
        // -------------------------------
        public IActionResult Subjects(int id)
{
    var role = HttpContext.Session.GetString("Role");
    if (role != "Admin")
        return RedirectToAction("Index", "Dashboard");

    // 1️⃣ Get course
    var course = _context.Courses
        .AsNoTracking()
        .FirstOrDefault(c => c.Id == id);

    if (course == null)
        return NotFound();

    // 2️⃣ Get all subjects
    var allSubjects = _context.Subjects
        .AsNoTracking()
        .OrderBy(s => s.Name)
        .ToList();

    // 3️⃣ Get already-selected subject IDs
    var selectedSubjectIds = _context.CourseSubjects
        .Where(cs => cs.CourseId == id)
        .Select(cs => cs.SubjectId)
        .ToList();

    // 4️⃣ CREATE THE VIEWMODEL (this is `vm`)
    var vm = new CourseSubjectsViewModel
    {
        Course = course,
        AllSubjects = allSubjects,
        SelectedSubjectIds = selectedSubjectIds
    };

    // 5️⃣ Pass it to the view
    return View(vm);
}
// POST: AdminCourses/Subjects
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Subjects(int courseId, List<int> selectedSubjectIds)
{
    var role = HttpContext.Session.GetString("Role");
    if (role != "Admin")
        return RedirectToAction("Index", "Dashboard");

    var course = _context.Courses.FirstOrDefault(c => c.Id == courseId);
    if (course == null)
        return NotFound();

    // Remove existing mappings
    var existing = _context.CourseSubjects
        .Where(cs => cs.CourseId == courseId);

    _context.CourseSubjects.RemoveRange(existing);

    // Add new mappings
    if (selectedSubjectIds != null && selectedSubjectIds.Any())
    {
        var mappings = selectedSubjectIds.Select(subjectId =>
            new CourseSubject
            {
                CourseId = courseId,
                SubjectId = subjectId
            });

        _context.CourseSubjects.AddRange(mappings);
    }

    _context.SaveChanges();

    TempData["Success"] = "Subjects updated successfully.";

    return RedirectToAction(nameof(Subjects), new { id = courseId });
}

        
        // -------------------------------
        // REMOVE SUBJECT
        // -------------------------------
        public IActionResult RemoveSubject(int courseId, int subjectId)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var mapping = _context.CourseSubjects
                .FirstOrDefault(cs => cs.CourseId == courseId && cs.SubjectId == subjectId);

            if (mapping != null)
            {
                _context.CourseSubjects.Remove(mapping);
                _context.SaveChanges();
                TempData["Success"] = "Subject removed from course.";
            }

            return RedirectToAction(nameof(Subjects), new { id = courseId });
        }
    }
}