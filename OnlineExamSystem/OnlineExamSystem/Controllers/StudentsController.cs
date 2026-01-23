using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
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

            // COURSES (unchanged, already working)
            var courses = _context.Courses
                .Where(c => c.IsApproved || c.Id == student.CourseId)
                .ToList();

            // COLLEGES — 🔥 FIXED LOGIC
            var colleges = _context.Colleges
                .Where(c =>
                    c.IsActive ||                 // all active colleges
                    c.Id == student.CollegeId    // OR currently selected
                )
                .ToList();

            ViewBag.Courses = courses;
            ViewBag.Colleges = colleges;

            // View vs Edit mode
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

            ViewBag.Courses = _context.Courses
                .Where(c => c.IsApproved || c.Id == student.CourseId)
                .ToList();

            ViewBag.Colleges = _context.Colleges
                .Where(c => c.IsActive || c.Id == student.CollegeId)
                .ToList();

            return View(student);
        }

        [HttpPost]
        public IActionResult EditProfile(Student model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var student = _context.Students
                .FirstOrDefault(s => s.UserId == userId.Value);

            if (student == null)
                return NotFound();

            // Duplicate roll number check
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

                ViewBag.Courses = _context.Courses
                    .Where(c => c.IsApproved || c.Id == model.CourseId)
                    .ToList();

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive || c.Id == model.CollegeId)
                    .ToList();

                return View(model);
            }

            student.Name = model.Name;
            student.CourseId = model.CourseId;
            student.CollegeId = model.CollegeId;
            student.RollNumber = model.RollNumber;

            student.IsProfileCompleted =
                !string.IsNullOrWhiteSpace(student.Name) &&
                student.CourseId.HasValue &&
                student.CollegeId.HasValue &&
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

            // Duplicate roll check (same college)
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

                ViewBag.Courses = _context.Courses
                    .Where(c => c.IsApproved || c.Id == model.CourseId)
                    .ToList();

                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive || c.Id == model.CollegeId)
                    .ToList();

                ViewBag.IsEditMode = true;
                return View(model);
            }

            // Save
            student.Name = model.Name;
            student.CourseId = model.CourseId;
            student.CollegeId = model.CollegeId;
            student.RollNumber = model.RollNumber;

            // Profile completion
            student.IsProfileCompleted =
                !string.IsNullOrWhiteSpace(student.Name) &&
                student.CourseId.HasValue &&
                student.CollegeId.HasValue &&
                !string.IsNullOrWhiteSpace(student.RollNumber);

            _context.SaveChanges();

            return RedirectToAction("Profile");
        }
    }
}
