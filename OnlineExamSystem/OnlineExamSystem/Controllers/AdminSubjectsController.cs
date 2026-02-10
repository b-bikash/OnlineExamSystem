using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace OnlineExamSystem.Controllers
{
    public class AdminSubjectsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminSubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // LIST
        // =========================
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var subjects = _context.Subjects
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToList();

            return View(subjects);
        }

        // =========================
        // CREATE
        // =========================
        
        // GET
[HttpGet]
public IActionResult Create()
{
    var role = HttpContext.Session.GetString("Role");
    if (role != "Admin")
        return Unauthorized();

    return View();
}

// POST
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Create(Subject model)
{
    var role = HttpContext.Session.GetString("Role");
    if (role != "Admin")
        return Unauthorized();

    if (string.IsNullOrWhiteSpace(model.Name))
    {
        ModelState.AddModelError("Name", "Subject name is required.");
        return View(model);
    }

    _context.Subjects.Add(model);
    _context.SaveChanges();

    TempData["Success"] = "Subject added successfully.";
    return RedirectToAction(nameof(Index));
}


        // =========================
        // EDIT
        // =========================
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var subject = _context.Subjects.Find(id);
            if (subject == null) return NotFound();

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Subject model)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Index", "Dashboard");

            var subject = _context.Subjects.Find(id);
            if (subject == null) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            subject.Name = model.Name;
            _context.SaveChanges();

            TempData["Success"] = "Subject updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // SAFE DELETE
        // =========================
        public IActionResult Delete(int id)
{
    if (HttpContext.Session.GetString("Role") != "Admin")
        return RedirectToAction("Index", "Dashboard");

    var subject = _context.Subjects
        .Include(s => s.CourseSubjects)
        .FirstOrDefault(s => s.Id == id);

    if (subject == null)
        return NotFound();

    // ❌ HARD BLOCK if exams exist
    bool hasExams = _context.Exams.Any(e => e.SubjectId == id);
    if (hasExams)
    {
        TempData["Error"] = "Cannot delete subject because exams already exist for it.";
        return RedirectToAction(nameof(Index));
    }

    // Safe cleanup
    if (subject.CourseSubjects.Any())
    {
        _context.CourseSubjects.RemoveRange(subject.CourseSubjects);
    }

    _context.Subjects.Remove(subject);
    _context.SaveChanges();

    TempData["Success"] = "Subject deleted successfully.";
    return RedirectToAction(nameof(Index));
}

    }
}
