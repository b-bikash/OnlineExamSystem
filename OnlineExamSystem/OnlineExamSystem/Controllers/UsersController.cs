using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Helpers;
using OnlineExamSystem.Models;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(AdminAuthorizeFilter))]
    public class UsersController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LIST USERS + EMAIL SEARCH
        public async Task<IActionResult> Index(string search, int? collegeId, string role)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var sessionUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == sessionUserId && u.IsActive);

            if (sessionUser == null || sessionUser.Role != "Admin")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var usersQuery = _context.Users
                .Include(u => u.College)
                .AsQueryable();

            // 🔎 Filter by email
            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery
                    .Where(u => u.Email.Contains(search));
            }

            // 🏫 Filter by College
            if (collegeId.HasValue)
            {
                usersQuery = usersQuery
                    .Where(u => u.CollegeId == collegeId.Value);
            }

            // 👤 Filter by Role
            if (!string.IsNullOrWhiteSpace(role))
            {
                usersQuery = usersQuery
                    .Where(u => u.Role == role);
            }

            ViewBag.Colleges = await _context.Colleges
                .Where(c => c.IsActive)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCollegeId = collegeId;
            ViewBag.SelectedRole = role;

            var users = await usersQuery.Where(c => c.IsActive).ToListAsync();

            return View(users);
        }

        // CREATE (GET)
        public IActionResult Create()
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var sessionUser = _context.Users
                .FirstOrDefault(u => u.Id == sessionUserId && u.IsActive);

            // 🔐 Only Global Admin can create users from dashboard
            if (sessionUser == null || sessionUser.Role != "Admin")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // 🔐 Load only active colleges
            ViewBag.Colleges = _context.Colleges
                .Where(c => c.IsActive)
                .ToList();

            return View(new User());   // 🔥 IMPORTANT (prevents null binding issues)
        }


        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User user, string FullName, string Password)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var sessionUser = _context.Users
                .FirstOrDefault(u => u.Id == sessionUserId && u.IsActive);

            // 🔐 Only Global Admin can create users
            if (sessionUser == null || sessionUser.Role != "Admin")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // 🔄 Helper to reload dropdown safely on validation failure
            void LoadColleges()
            {
                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();
            }

            // 🔐 Role validation
            if (string.IsNullOrWhiteSpace(user.Role))
            {
                ViewBag.Error = "Role is required.";
                LoadColleges();
                return View(user);
            }

            // 🔐 College validation (Admin must NOT have college, others MUST have)
            if (user.Role == "Admin")
            {
                user.CollegeId = null; // enforce global admin
            }
            else if (user.CollegeId == null)
            {
                ViewBag.Error = "College must be selected.";
                LoadColleges();
                return View(user);
            }

            // 🔐 Unique username
            if (_context.Users.Any(u => u.Username == user.Username))
            {
                ViewBag.Error = "Username already exists.";
                LoadColleges();
                return View(user);
            }

            // 🔐 Unique email
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email already exists.";
                LoadColleges();
                return View(user);
            }

            // 🔐 One TeacherAdmin per college enforcement
            if (user.Role == "TeacherAdmin")
            {
                bool exists = _context.Users.Any(u =>
                    u.Role == "TeacherAdmin" &&
                    u.CollegeId == user.CollegeId);

                if (exists)
                {
                    ViewBag.Error = "This college already has a TeacherAdmin.";
                    LoadColleges();
                    return View(user);
                }
            }

            // 🔐 Secure fields (never trust client)
            user.PasswordHash = PasswordHelper.HashPassword(Password);
            user.IsActive = true;

            // 🔐 Persist user first (to generate UserId)
            _context.Users.Add(user);
            _context.SaveChanges();

            // 🔐 Create related domain entity (STRICT tenant binding)
            if (user.Role == "Student")
            {
                _context.Students.Add(new Student
                {
                    UserId = user.Id,
                    Name = FullName,
                    IsProfileCompleted = false,
                    CollegeId = user.CollegeId.Value // safe because validated above
                });
            }
            else if (user.Role == "Teacher" || user.Role == "TeacherAdmin")
            {
                _context.Teachers.Add(new Teacher
                {
                    UserId = user.Id,
                    Name = FullName,
                    CollegeId = user.CollegeId.Value
                });
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        // EDIT (GET)
        public IActionResult Edit(int id)
        {
            ViewBag.Colleges = _context.Colleges
                .Where(c => c.IsActive)
                .ToList();

            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var sessionUser = _context.Users
                .FirstOrDefault(u => u.Id == sessionUserId && u.IsActive);

            if (sessionUser == null || sessionUser.Role != "Admin")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
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
            else if (user.Role == "Teacher" || user.Role == "TeacherAdmin")
            {
                var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.Id);
                ViewBag.FullName = teacher?.Name;
            }
            //else if (user.Role == "TeacherAdmin")
            //{
              //  var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.Id);
                //ViewBag.FullName = teacher?.Name;
            //}

            return View(user);
        }

        // EDIT (POST)
        [HttpPost]
        public IActionResult Edit(
    int id,
    string FullName,
    string Email,
    string Password,
    string Role,
    int? CollegeId)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var sessionUser = _context.Users
                .FirstOrDefault(u => u.Id == sessionUserId && u.IsActive);

            if (sessionUser == null || sessionUser.Role != "Admin")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            void LoadColleges()
            {
                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return RedirectToAction("Index");

            // 🔐 Prevent Admin from being downgraded
            if (user.Role == "Admin" && Role != "Admin")
            {
                ViewBag.Error = "Admin role cannot be changed.";
                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                return View(user);
            }

            // 🔐 Prevent Admin from changing their own role
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            if (currentUserId == user.Id && user.Role == "Admin" && Role != "Admin")
            {
                ViewBag.Error = "You cannot change your own Admin role.";
                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();

                return View(user);
            }

                if (_context.Users.Any(u => u.Email == Email && u.Id != id))
            {
                ViewBag.Error = "Email already exists.";
                LoadColleges();
                return View(user);
            }

            // College required for non-admin roles
            if (Role != "Admin" && CollegeId == null)
            {
                ViewBag.Error = "College must be selected.";
                LoadColleges();
                return View(user);
            }

            // 🔐 Enforce one TeacherAdmin per college
            if (Role == "TeacherAdmin")
            {
                bool exists = _context.Users
                    .Any(u => u.Role == "TeacherAdmin" &&
                              u.CollegeId == CollegeId.Value &&
                              u.Id != id);

                if (exists)
                {
                    ViewBag.Error = "This college already has a TeacherAdmin.";
                    LoadColleges();
                    return View(user);
                }
            }

            string oldRole = user.Role;

            user.Email = Email;
            user.Role = Role;
            user.CollegeId = (Role == "Admin") ? null : CollegeId;

            if (!string.IsNullOrWhiteSpace(Password))
            {
                user.PasswordHash = PasswordHelper.HashPassword(Password);
            }

            // 🔁 Handle role switching safely

            if (oldRole == "Student" && Role != "Student")
            {
                var oldStudent = _context.Students.FirstOrDefault(s => s.UserId == user.Id);
                if (oldStudent != null)
                    _context.Students.Remove(oldStudent);
            }

            if ((oldRole == "Teacher" || oldRole == "TeacherAdmin") &&
                Role != "Teacher" && Role != "TeacherAdmin")
            {
                var oldTeacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.Id);
                if (oldTeacher != null)
                    _context.Teachers.Remove(oldTeacher);
            }

            if (Role == "Student")
            {
                var student = _context.Students.FirstOrDefault(s => s.UserId == user.Id);

                if (student == null)
                {
                    _context.Students.Add(new Student
                    {
                        UserId = user.Id,
                        Name = FullName,
                        IsProfileCompleted = false,
                        CollegeId = CollegeId.Value
                    });
                }
                else
                {
                    student.Name = FullName;
                    student.CollegeId = CollegeId.Value;
                }
            }
            else if (Role == "Teacher" || Role == "TeacherAdmin")
            {
                var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == user.Id);

                if (teacher == null)
                {
                    _context.Teachers.Add(new Teacher
                    {
                        UserId = user.Id,
                        Name = FullName,
                        CollegeId = CollegeId.Value
                    });
                }
                else
                {
                    teacher.Name = FullName;
                    teacher.CollegeId = CollegeId.Value;
                }
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // DELETE (POST)
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId == null)
                return RedirectToAction("Login", "Account");

            var sessionUser = _context.Users
                .FirstOrDefault(u => u.Id == sessionUserId && u.IsActive);

            if (sessionUser == null || sessionUser.Role != "Admin")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            user.IsActive = false;
            _context.Users.Update(user);
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
