using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Helpers;
using OnlineExamSystem.Models;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // LOGIN
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter username/email and password.";
                return View();
            }

            var hashedPassword = PasswordHelper.HashPassword(password);

            var user = _context.Users.FirstOrDefault(u =>
                (u.Username == usernameOrEmail || u.Email == usernameOrEmail)
                && u.PasswordHash == hashedPassword
                && u.IsActive
            );

            if (user == null)
            {
                ViewBag.Error = "Invalid login credentials.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("Index", "Dashboard");
        }

        // =========================
        // REGISTER
        // =========================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Allow only Student or Teacher
            if (model.Role != "Student" && model.Role != "Teacher")
            {
                ModelState.AddModelError("", "Invalid role selected.");
                return View(model);
            }

            // Check email uniqueness
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Role = model.Role,
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // ✅ CREATE STUDENT ROW HERE (EMPTY)
            if (user.Role == "Student")
            {
                var student = new Student
                {
                    UserId = user.Id,
                    Name = user.Username
                    // CourseId, CollegeId, RollNumber = NULL
                };

                _context.Students.Add(student);
            }
            else if (user.Role == "Teacher")
            {
                var teacher = new Teacher
                {
                    UserId = user.Id,
                    Name = user.Username
                };

                _context.Teachers.Add(teacher);
            }

            _context.SaveChanges();

            return RedirectToAction("Login");
        }


        // =========================
        // LOGOUT
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
