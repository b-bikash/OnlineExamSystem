using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;
using OnlineExamSystem.Helpers;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    public class UsersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST USERS + EMAIL SEARCH
        public IActionResult Index(string searchEmail)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                usersQuery = usersQuery
                    .Where(u => u.Email.Contains(searchEmail));
            }

            ViewBag.SearchEmail = searchEmail;

            var users = usersQuery.ToList();
            return View(users);
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // CREATE (POST)
        [HttpPost]
        public IActionResult Create(
            string FullName,
            string Username,
            string Email,
            string Password,
            string Role)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            if (_context.Users.Any(u => u.Username == Username))
            {
                ViewBag.Error = "Username already exists";
                return View();
            }

            if (_context.Users.Any(u => u.Email == Email))
            {
                ViewBag.Error = "Email already exists";
                return View();
            }

            var user = new User
            {
                Username = Username,
                Email = Email,
                Role = Role,
                PasswordHash = PasswordHelper.HashPassword(Password),
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // 🔹 MATCH REGISTER LOGIC
            if (Role == "Student")
            {
                var student = new Student
                {
                    UserId = user.Id,
                    Name = FullName,
                    IsProfileCompleted = false
                };

                _context.Students.Add(student);
            }
            else if (Role == "Teacher")
            {
                var teacher = new Teacher
                {
                    UserId = user.Id,
                    Name = FullName
                };

                _context.Teachers.Add(teacher);
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        // EDIT (GET)
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            // 🔹 Load Full Name if Student / Teacher
            if (user.Role == "Student")
            {
                var student = _context.Students.FirstOrDefault(s => s.UserId == user.Id);
                ViewBag.FullName = student?.Name;
            }
            else if (user.Role == "Teacher")
            {
                var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.Id);
                ViewBag.FullName = teacher?.Name;
            }

            return View(user);
        }

        // EDIT (POST)
        [HttpPost]
        public IActionResult Edit(
            int id,
            string FullName,
            string Email,
            string Password,
            string Role)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            if (_context.Users.Any(u => u.Email == Email && u.Id != id))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }

            user.Email = Email;
            user.Role = Role;

            if (!string.IsNullOrWhiteSpace(Password))
            {
                user.PasswordHash = PasswordHelper.HashPassword(Password);
            }

            // 🔹 UPDATE FULL NAME
            if (Role == "Student")
            {
                var student = _context.Students.FirstOrDefault(s => s.UserId == user.Id);
                if (student != null)
                {
                    student.Name = FullName;
                }
            }
            else if (Role == "Teacher")
            {
                var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.Id);
                if (teacher != null)
                {
                    teacher.Name = FullName;
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // DELETE (GET)
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == id)
            {
                return RedirectToAction("Index");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            return View(user);
        }

        // DELETE (POST)
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // TOGGLE ACTIVE STATUS
        public IActionResult ToggleStatus(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
