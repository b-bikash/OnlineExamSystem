using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    public class TeachersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public TeachersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: Teacher Profile
        // =========================
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value && u.Role == "Teacher");

            if (user == null)
                return Unauthorized();

            var teacher = _context.Teachers
                .AsNoTracking()
                .FirstOrDefault(t => t.UserId == user.Id);

            if (teacher == null)
                return NotFound();

            ViewBag.Username = user.Username;
            ViewBag.Email = user.Email;

            return View(teacher);
        }

        // =========================
        // POST: Teacher Profile
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(string name, string username, string email)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .FirstOrDefault(u => u.Id == userId.Value && u.Role == "Teacher");

            if (user == null)
                return Unauthorized();

            var teacher = _context.Teachers
                .FirstOrDefault(t => t.UserId == user.Id);

            if (teacher == null)
                return NotFound();

            // Username uniqueness
            bool usernameExists = _context.Users.Any(u =>
                u.Username == username &&
                u.Id != user.Id
            );

            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Username already exists.");
            }

            // Email uniqueness
            bool emailExists = _context.Users.Any(u =>
                u.Email == email &&
                u.Id != user.Id
            );

            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Username = username;
                ViewBag.Email = email;

                return View("Profile", teacher);
            }

            // ✅ SAVE
            teacher.Name = name;
            user.Username = username;
            user.Email = email;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Profile updated successfully.";

            // ✅ REDIRECT TO TEACHER DASHBOARD
            return RedirectToAction("Index", "Dashboard");
        }
    }
}