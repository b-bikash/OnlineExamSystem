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
        // INDEX
        // =========================
        // GET: AdminSubjects + SEARCH
        public async Task<IActionResult> Index(string search, int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var subjectsQuery = _context.Subjects.Where(s => s.IsActive).Include(s => s.College).AsQueryable();

            // 🔐 TeacherAdmin → Only own college
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                subjectsQuery = subjectsQuery
                    .Where(s => s.CollegeId == sessionCollegeId.Value);
            }
            else if (role == "Admin")
            {
                // Optional college filter
                if (collegeId.HasValue)
                {
                    subjectsQuery = subjectsQuery
                        .Where(s => s.CollegeId == collegeId.Value);
                }

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                ViewBag.SelectedCollegeId = collegeId;
            }

            // 🔎 Search by Subject Name
            if (!string.IsNullOrWhiteSpace(search))
            {
                subjectsQuery = subjectsQuery.Where(s =>
                    s.Name != null && s.Name.Contains(search));
            }

            ViewBag.Search = search;

            return View(await subjectsQuery.ToListAsync());
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
        public async Task<IActionResult> Create(Subject model)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Subject name is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError("Code", "Subject code is required.");
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

            // 🔎 Duplicate Subject Code check (College-wise, case-insensitive)
            bool codeExists = await _context.Subjects.AnyAsync(s =>
                s.CollegeId == model.CollegeId &&
                s.Code.ToLower() == model.Code.ToLower());

            if (codeExists)
            {
                ModelState.AddModelError("Code", "Subject code already exists in this college.");

                if (role == "Admin")
                {
                    ViewBag.Colleges = _context.Colleges
                        .Where(c => c.IsActive)
                        .ToList();
                }

                return View(model);
            }

            _context.Subjects.Add(model);
            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> Edit(int id, Subject model)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == id);
            if (subject == null)
                return NotFound();

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null || subject.CollegeId != sessionCollegeId.Value)
                    return Unauthorized();

                model.CollegeId = sessionCollegeId.Value;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError("Name", "Subject name is required.");

            if (string.IsNullOrWhiteSpace(model.Code))
                ModelState.AddModelError("Code", "Subject code is required.");

            if (!ModelState.IsValid)
                return View(model);

            // 🔎 Duplicate Subject Code check (College-wise, case-insensitive)
            bool codeDuplicate = await _context.Subjects.AnyAsync(s =>
                s.Id != id &&
                s.CollegeId == subject.CollegeId &&
                s.Code.ToLower() == model.Code.ToLower());

            if (codeDuplicate)
            {
                ModelState.AddModelError("Code", "Subject code already exists in this college.");
                return View(model);
            }

            // Update fields
            subject.Name = model.Name;
            subject.Code = model.Code;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Subject updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // SAFE DELETE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var subject = await _context.Subjects
                .Include(s => s.CourseSubjects)
                .Include(s => s.TeacherSubjects)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || subject.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            // 🔐 Block if exams exist
            bool hasExams = await _context.Exams.AnyAsync(e => e.SubjectId == id);
            if (hasExams)
            {
                TempData["Error"] = "Cannot delete subject because exams already exist for it.";
                return RedirectToAction(nameof(Index));
            }

            // Remove Teacher-Subject mappings
            if (subject.TeacherSubjects != null && subject.TeacherSubjects.Any())
            {
                _context.TeacherSubjects.RemoveRange(subject.TeacherSubjects);
            }

            // Remove Course-Subject mappings
            if (subject.CourseSubjects != null && subject.CourseSubjects.Any())
            {
                _context.CourseSubjects.RemoveRange(subject.CourseSubjects);
            }

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Subject deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}