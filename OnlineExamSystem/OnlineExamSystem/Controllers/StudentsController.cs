using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using OnlineExamSystem.Models.ViewModels;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(SessionValidationFilter))]
    public class StudentsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: Profile (View / Edit)
        // =========================
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Students
                .Include(s => s.Course)
                .Include(s => s.College)
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
                return NotFound();

            var courses = _context.Courses.ToList();

            var colleges = _context.Colleges
                .Where(c =>
                    c.IsActive ||
                    c.Id == student.CollegeId
                )
                .ToList();

            ViewBag.Courses = courses;
            ViewBag.Colleges = colleges;

            ViewBag.IsEditMode = !student.IsProfileCompleted;

            return View(student);
        }

        public IActionResult EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Students
                .Include(s => s.Course)
                .Include(s => s.College)
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
                return NotFound();

            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (sessionCollegeId == null)
                return Unauthorized();

            var collegeName = _context.Colleges
                .Where(c => c.Id == sessionCollegeId.Value)
                .Select(c => c.Name)
                .FirstOrDefault();

            ViewBag.CollegeName = collegeName; 
            
            ViewBag.Courses = _context.Courses
                .Where(c => c.CollegeId == sessionCollegeId.Value)
                .ToList();

            ViewBag.Colleges = _context.Colleges
                .Where(c => c.IsActive || c.Id == student.CollegeId)
                .ToList();

            var vm = new StudentProfileViewModel
            {
                Name = student.Name,
                CourseId = student.CourseId,
                RollNumber = student.RollNumber
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult EditProfile(StudentProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Students
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
                return NotFound();

            // 🔐 Validate Course belongs to college
            bool validCourse = _context.Courses.Any(c =>
                c.Id == model.CourseId &&
                c.CollegeId == sessionCollegeId.Value);

            if (!validCourse)
            {
                ModelState.AddModelError("CourseId", "Invalid course selection.");
            }

            // 🔐 Validate Roll uniqueness
            bool rollExists = _context.Students.Any(s =>
                s.Id != student.Id &&
                s.CollegeId == sessionCollegeId.Value &&
                s.RollNumber == model.RollNumber);

            if (rollExists)
            {
                ModelState.AddModelError(
                    "RollNumber",
                    "This roll number already exists in your college.");
            }

            if (!ModelState.IsValid)
            {
                // Reload courses
                ViewBag.Courses = _context.Courses
                    .Where(c => c.CollegeId == sessionCollegeId.Value)
                    .ToList();

                // Reload college name
                ViewBag.CollegeName = _context.Colleges
                    .Where(c => c.Id == sessionCollegeId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefault();

                return View(model);
            }

            // Save safely
            student.Name = model.Name;
            student.CourseId = model.CourseId;
            student.RollNumber = model.RollNumber;
            student.CollegeId = sessionCollegeId.Value;

            student.IsProfileCompleted =
                !string.IsNullOrWhiteSpace(student.Name) &&
                student.CourseId.HasValue &&
                !string.IsNullOrWhiteSpace(student.RollNumber);

            _context.SaveChanges();

            return RedirectToAction("Profile");
        }

        // =========================
        // POST: Save Profile
        // =========================
        [HttpPost]
        public IActionResult Profile(Student model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Students
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
                return NotFound();

            bool rollExists = _context.Students.Any(s =>
                s.Id != student.Id &&
                s.CollegeId == model.CollegeId &&
                s.RollNumber == model.RollNumber
            );

            if (rollExists)
            {
                ModelState.AddModelError(
                    "RollNumber",
                    "This roll number already exists for the selected college."
                );

                ViewBag.Courses = _context.Courses.ToList();

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive || c.Id == model.CollegeId)
                    .ToList();

                ViewBag.IsEditMode = true;
                return View(model);
            }

            student.Name = model.Name;
            student.CourseId = model.CourseId;
            student.CollegeId = model.CollegeId;
            student.RollNumber = model.RollNumber;

            student.IsProfileCompleted =
                !string.IsNullOrWhiteSpace(student.Name) &&
                student.CourseId.HasValue &&
                student.CollegeId > 0 &&
                !string.IsNullOrWhiteSpace(student.RollNumber);

            _context.SaveChanges();

            return RedirectToAction("Profile");
        }
    }
}