using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Models.ViewModels;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(AdminAuthorizeFilter))]
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
        public IActionResult Index(string search, int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var coursesQuery = _context.Courses.AsQueryable();

            // 🔐 TeacherAdmin → Only own college
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                coursesQuery = coursesQuery
                    .Where(c => c.CollegeId == sessionCollegeId.Value);
            }
            else if (role == "Admin")
            {
                // 🏫 Optional college filter for Admin
                if (collegeId.HasValue)
                {
                    coursesQuery = coursesQuery
                        .Where(c => c.CollegeId == collegeId.Value);
                }

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                ViewBag.SelectedCollegeId = collegeId;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                coursesQuery = coursesQuery
                    .Where(c => c.Name != null && c.Name.Contains(search));
            }

            ViewBag.Search = search;

            return View(coursesQuery.ToList());
        }

        // -------------------------------
        // CREATE
        // -------------------------------
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

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
        public IActionResult Create(Course model)
        {
            Console.WriteLine("COURSE CREATE POST HIT"); 
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Course name is required.");
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

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();
            }

            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine("Model Error: " + error.ErrorMessage);
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

            bool exists = _context.Courses.Any(c =>
                c.Name.ToLower() == model.Name.ToLower() &&
                c.CollegeId == model.CollegeId);

            if (exists)
            {
                ModelState.AddModelError("Name",
                    "A course with this name already exists in this college.");

                if (role == "Admin")
                {
                    ViewBag.Colleges = _context.Colleges
                        .Where(c => c.IsActive)
                        .ToList();
                }

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
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || course.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            return View(course);
        }

        [HttpPost]
        public IActionResult Edit(int id, Course model)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.FirstOrDefault(c => c.Id == id);
            if (course == null)
                return NotFound();

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null || course.CollegeId != sessionCollegeId.Value)
                    return Unauthorized();

                model.CollegeId = sessionCollegeId.Value;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Course name is required.");
                return View(course);
            }

            bool duplicate = _context.Courses.Any(c =>
                c.Id != id &&
                c.Name.ToLower() == model.Name.ToLower() &&
                c.CollegeId == model.CollegeId);

            if (duplicate)
            {
                ModelState.AddModelError("Name", "A course with this name already exists in this college.");
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            IQueryable<Course> query = _context.Courses;

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                query = query.Where(c => c.CollegeId == sessionCollegeId.Value);
            }

            var course = query.FirstOrDefault(c => c.Id == id);

            if (course == null)
                return NotFound();

            // 🔐 Check dependent Students
            bool hasStudents = _context.Students.Any(s => s.CourseId == id);

            if (hasStudents)
            {
                TempData["Error"] = "Cannot delete this course because students are assigned to it.";
                return RedirectToAction(nameof(Index));
            }

            _context.Courses.Remove(course);
            _context.SaveChanges();

            TempData["Success"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // COURSE ⇄ SUBJECT MANAGEMENT
        // =====================================================

        public IActionResult Subjects(int id)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var course = _context.Courses
                .FirstOrDefault(c => c.Id == id);

            if (course == null)
                return NotFound();

            // 🔐 SECURITY: TeacherAdmin cannot access other college course
            if (role == "TeacherAdmin" && course.CollegeId != sessionCollegeId)
                return Unauthorized();

            // ✅ Only load subjects of that course's college
            var subjects = _context.Subjects
                .Where(s => s.CollegeId == course.CollegeId)
                .ToList();

            // ✅ Already assigned subject IDs
            var selectedSubjectIds = _context.CourseSubjects
                .Where(cs => cs.CourseId == course.Id)
                .Select(cs => cs.SubjectId)
                .ToList();

            var viewModel = new CourseSubjectsViewModel
            {
                Course = course,
                AllSubjects = subjects,
                SelectedSubjectIds = selectedSubjectIds
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Subjects(int courseId, List<int> selectedSubjectIds)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || course.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

            var existing = _context.CourseSubjects
                .Where(cs => cs.CourseId == courseId);

            _context.CourseSubjects.RemoveRange(existing);

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

        public IActionResult RemoveSubject(int courseId, int subjectId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            var course = _context.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
                return NotFound();

            if (role == "TeacherAdmin" &&
                (sessionCollegeId == null || course.CollegeId != sessionCollegeId.Value))
                return Unauthorized();

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