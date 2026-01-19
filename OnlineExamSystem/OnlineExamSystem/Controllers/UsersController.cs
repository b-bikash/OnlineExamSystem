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

        // LIST USERS
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var users = _context.Users.ToList();
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
        public IActionResult Create(string Username, string Email, string Password, string Role)
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
                PasswordHash = PasswordHelper.HashPassword(Password)
            };

            _context.Users.Add(user);
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

            return View(user);
        }

        // EDIT (POST)
        [HttpPost]
        public IActionResult Edit(int id, string Email, string Password, string Role)
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

            // 🔒 Duplicate email check (excluding current user)
            if (_context.Users.Any(u => u.Email == Email && u.Id != id))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }

            user.Email = Email;
            user.Role = Role;

            // Update password only if provided
            if (!string.IsNullOrWhiteSpace(Password))
            {
                user.PasswordHash = PasswordHelper.HashPassword(Password);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }


        // DELETE (GET - Confirmation)
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            // Prevent deleting yourself
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
    }
}
