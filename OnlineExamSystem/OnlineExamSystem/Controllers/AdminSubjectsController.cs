using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(AdminAuthorizeFilter))]
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
        public IActionResult Index(int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var query = _context.Subjects.AsNoTracking();

            // 🔐 TeacherAdmin → Only own college
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                query = query.Where(s => s.CollegeId == sessionCollegeId.Value);
            }
            else if (role == "Admin")
            {
                // 🏫 Optional college filter for Admin
                if (collegeId.HasValue)
                {
                    query = query.Where(s => s.CollegeId == collegeId.Value);
                }

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                ViewBag.SelectedCollegeId = collegeId;
            }

            var subjects = query
                .OrderBy(s => s.Name)
                .ToList();

            return View(subjects);
        }

        // =========================
        // CREATE
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            if (role == "Admin")
            {
                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();
            }

            return View();
        }

        [HttpPost]
        public IActionResult Create(Subject model)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Subject name is required.");
            }

            // 🔐 Multi-tenant enforcement
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                model.CollegeId = sessionCollegeId.Value;
            }
            else if (role == "Admin")
            {
                if (model.CollegeId == 0)
                {
                    ModelState.AddModelError("CollegeId", "College selection is required.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (role == "Admin")
                {
                    ViewBag.Colleges = _context.Colleges
                        .Where(c => c.IsActive)
                        .ToList();
                }

                return View(model);
            }

            bool exists = _context.Subjects.Any(s =>
                s.Name.ToLower() == model.Name.ToLower() &&
                s.CollegeId == model.CollegeId);

            if (exists)
            {
                ModelState.AddModelError("Name", "Subject already exists in this college.");

                if (role == "Admin")
                {
                    ViewBag.Colleges = _context.Colleges
                        .Where(c => c.IsActive)
                        .ToList();
                }

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
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var subject = _context.Subjects.FirstOrDefault(s => s.Id == id);
            if (subject == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || subject.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Subject model)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var subject = _context.Subjects.FirstOrDefault(s => s.Id == id);
            if (subject == null)
                return NotFound();

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null || subject.CollegeId != sessionCollegeId.Value)
                    return Unauthorized();

                model.CollegeId = sessionCollegeId.Value;
            }

            if (!ModelState.IsValid)
                return View(model);

            bool duplicate = _context.Subjects.Any(s =>
                s.Id != id &&
                s.Name.ToLower() == model.Name.ToLower() &&
                s.CollegeId == model.CollegeId);

            if (duplicate)
            {
                ModelState.AddModelError("Name", "Subject already exists in this college.");
                return View(subject);
            }

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
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var subject = _context.Subjects
                .Include(s => s.CourseSubjects)
                .FirstOrDefault(s => s.Id == id);

            if (subject == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || subject.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            bool hasExams = _context.Exams.Any(e => e.SubjectId == id);
            if (hasExams)
            {
                TempData["Error"] = "Cannot delete subject because exams already exist for it.";
                return RedirectToAction(nameof(Index));
            }

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