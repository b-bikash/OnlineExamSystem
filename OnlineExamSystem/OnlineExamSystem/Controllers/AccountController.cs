using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Helpers;
using OnlineExamSystem.Models;
using System.Linq;
using OnlineExamSystem.Services.Email;
using System.Threading.Tasks;

namespace OnlineExamSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            usernameOrEmail = usernameOrEmail.Trim().ToLower();
            var hashedPassword = PasswordHelper.HashPassword(password);

            var user = _context.Users.FirstOrDefault(u =>
                (u.Username.ToLower() == usernameOrEmail || u.Email.ToLower() == usernameOrEmail)
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

            // ✅ STORE COLLEGE ID (except Admin)
            if (user.Role != "Admin" && user.CollegeId.HasValue)
            {
                HttpContext.Session.SetInt32("CollegeId", user.CollegeId.Value);
            }

            string fullName = user.Username; // fallback

            if (user.Role == "Student")
            {
                var student = _context.Students
                    .FirstOrDefault(s => s.UserId == user.Id);

                if (student != null)
                    fullName = student.Name;
                    HttpContext.Session.SetInt32("CollegeId", student.CollegeId);
            }
            else if (user.Role == "Teacher")
            {
                var teacher = _context.Teachers
                    .FirstOrDefault(t => t.UserId == user.Id);

                if (teacher != null)
                {
                    fullName = teacher.Name;
                    HttpContext.Session.SetInt32("TeacherId", teacher.Id);
                    HttpContext.Session.SetInt32("CollegeId", teacher.CollegeId);
                }
            }

            HttpContext.Session.SetString("FullName", fullName);

            return RedirectToAction("Index", "Dashboard");
        }

        // =========================
        // REGISTER
        // =========================
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Colleges = _context.Colleges.ToList();
            return View();
        }

        [HttpPost]
public IActionResult Register(RegisterViewModel model)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Colleges = _context.Colleges.ToList();
        return View(model);
    }

    if (model.Role != "Student" && model.Role != "Teacher")
    {
        ModelState.AddModelError("", "Invalid role selected.");
        ViewBag.Colleges = _context.Colleges.ToList();
        return View(model);
    }
            model.Email = model.Email.Trim().ToLower();
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Email.ToLower() == model.Email);

            if (existingUser != null)
    {
        ModelState.AddModelError("", "Email already registered.");
        ViewBag.Colleges = _context.Colleges.ToList();
        return View(model);
    }

    // ✅ TRANSACTION START
    using var transaction = _context.Database.BeginTransaction();

    try
    {
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email.ToLower(),
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    Role = model.Role,
                    CollegeId = model.Role == "Admin" ? null : model.CollegeId,
                    IsActive = true
                };

                _context.Users.Add(user);
        _context.SaveChanges(); // generates user.Id

        if (user.Role == "Student")
        {
                    if (model.CollegeId == null)
                    {
                        throw new Exception("CollegeId is required for Student.");
                    }

                    var student = new Student
                    {
                        UserId = user.Id,
                        Name = model.Name,
                        CollegeId = model.CollegeId.Value
                    };

                    _context.Students.Add(student);
        }
        else if (user.Role == "Teacher")
        {
            // 🔴 REQUIRED VALIDATION
            if (model.CollegeId == null)
            {
                throw new Exception("CollegeId is required for Teacher.");
            }

            var teacher = new Teacher
            {
                UserId = user.Id,
                Name = model.Name,
                CollegeId = model.CollegeId.Value // ✅ FIX
            };

            _context.Teachers.Add(teacher);
        }

        _context.SaveChanges(); // student / teacher
        transaction.Commit();

        return RedirectToAction("Login");
    }
    catch
    {
        transaction.Rollback();
        ModelState.AddModelError("", "Registration failed. Please try again.");
        ViewBag.Colleges = _context.Colleges.ToList();
        return View(model);
    }
}
        // =========================
        // FORGOT PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(OnlineExamSystem.Models.ViewModels.ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLower();

            var user = _context.Users
                .FirstOrDefault(u => u.Email.ToLower() == email && u.IsActive);

            // ⚠️ IMPORTANT: Do NOT reveal if user exists or not
            if (user != null)
            {
                // 🔐 Generate raw token
                var token = Guid.NewGuid().ToString();

                // 🔒 Hash token before storing
                var hashedToken = PasswordHelper.HashPassword(token);

                user.PasswordResetToken = hashedToken;
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

                _context.SaveChanges();

                // 🔗 Build reset link
                var resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = token },
                    Request.Scheme
                );

                // 📧 Email content
                var subject = "Reset Your Password";
                var body = $@"
        <p>Hello {user.Username},</p>
        <p>Click the link below to reset your password:</p>
        <p><a href='{resetLink}'>Reset Password</a></p>
        <p>This link will expire in 30 minutes.</p>
    ";

                // 📤 Send email
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }

            ViewBag.Message = "If an account with this email exists, a password reset link will be sent.";

            return View();
        }
        // =========================
        // RESET PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var model = new OnlineExamSystem.Models.ViewModels.ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(OnlineExamSystem.Models.ViewModels.ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 🔒 Hash incoming token
            var hashedToken = PasswordHelper.HashPassword(model.Token);

            var user = _context.Users.FirstOrDefault(u =>
                u.PasswordResetToken == hashedToken &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow
            );

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid or expired token.");
                return View(model);
            }

            // 🔐 Update password
            user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);

            // 🧹 Clear token
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            _context.SaveChanges();

            TempData["Success"] = "Password reset successful. Please login.";

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
