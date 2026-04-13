using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(SessionValidationFilter))]
    public class ExamAttemptsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ExamAttemptsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // -------------------------------
        // COMPATIBILITY TEST (PRE-FLIGHT)
        // -------------------------------
        [HttpGet]
        public IActionResult CompatibilityTest(int examId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null)
                return Unauthorized();

            if (!student.IsProfileCompleted)
                return RedirectToAction("Profile", "Students");

            var exam = _context.Exams
                .Include(e => e.Subject)
                    .ThenInclude(s => s.CourseSubjects)
                .AsNoTracking()
                .FirstOrDefault(e =>
                    e.Id == examId &&
                    e.CollegeId == student.CollegeId &&
                    e.Subject.CourseSubjects.Any(cs => cs.CourseId == student.CourseId)
                );

            if (exam == null)
                return Unauthorized();

            if (!exam.StartDateTime.HasValue || !exam.EndDateTime.HasValue)
            {
                TempData["ErrorMessage"] = "This exam is not scheduled yet.";
                return RedirectToAction("Index", "Exams");
            }

            var now = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime();

            if (now < exam.StartDateTime.Value)
            {
                TempData["ErrorMessage"] = "This exam has not started yet.";
                return RedirectToAction("Index", "Exams");
            }

            if (now > exam.EndDateTime.Value)
            {
                TempData["ErrorMessage"] = "This exam has already ended.";
                return RedirectToAction("Index", "Exams");
            }

            return View(exam);
        }

        // -------------------------------
        // START EXAM
        // -------------------------------
        [HttpGet]
        public IActionResult Start(int examId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null)
                return Unauthorized();

            if (!student.IsProfileCompleted)
                return RedirectToAction("Profile", "Students");

            var exam = _context.Exams
                .Include(e => e.Subject)
                    .ThenInclude(s => s.CourseSubjects)
                .Include(e => e.CreatedByTeacher)
                .AsNoTracking()
                .FirstOrDefault(e =>
                    e.Id == examId &&
                    e.CollegeId == student.CollegeId &&
                    e.Subject.CourseSubjects.Any(cs => cs.CourseId == student.CourseId)
                );

            if (exam == null)
                return Unauthorized();

            if (!exam.StartDateTime.HasValue || !exam.EndDateTime.HasValue)
            {
                TempData["ErrorMessage"] = "This exam is not scheduled yet.";
                return RedirectToAction("Index", "Exams");
            }

            var now = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime();

            if (now < exam.StartDateTime.Value)
            {
                TempData["ErrorMessage"] = "This exam has not started yet.";
                return RedirectToAction("Index", "Exams");
            }

            if (now > exam.EndDateTime.Value)
            {
                TempData["ErrorMessage"] = "This exam has already ended.";
                return RedirectToAction("Index", "Exams");
            }

            var existingAttempt = _context.ExamAttempts
                .AsNoTracking()
                .FirstOrDefault(a =>
                    a.ExamId == examId &&
                    a.StudentId == student.Id &&
                    a.CollegeId == student.CollegeId   // 🔐 STRICT TENANT FILTER
                );

            if (existingAttempt != null)
                return RedirectToAction("AttemptAll", new { attemptId = existingAttempt.Id });

            // 🔐 DEFENSIVE TENANT VALIDATION
            if (exam.CollegeId != student.CollegeId)
                return Unauthorized();

            var attempt = new ExamAttempt
            {
                ExamId = examId,
                StudentId = student.Id,
                CollegeId = student.CollegeId,  // 🔐 MANDATORY TENANT WRITE
                StartTime = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime()
            };

            _context.ExamAttempts.Add(attempt);
            _context.SaveChanges();

            return RedirectToAction("AttemptAll", new { attemptId = attempt.Id });
        }

        // -------------------------------
        // ATTEMPT (GOOGLE FORM STYLE)
        // -------------------------------
        [HttpGet]
        public IActionResult AttemptAll(int attemptId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null || student.CollegeId != sessionCollegeId.Value)
                return Unauthorized();

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(a => a.StudentAnswers)
                .FirstOrDefault(a =>
                    a.Id == attemptId &&
                    a.StudentId == student.Id &&                  // 🔐 Ownership check
                    a.CollegeId == student.CollegeId              // 🔐 Tenant isolation
                );

            if (attempt == null)
                return Unauthorized();

            if (attempt.EndTime != null)
                return RedirectToAction("Index", "Exams");

            return View(attempt);
        }

        // -------------------------------
        // SAVE ANSWER
        // -------------------------------
        [HttpPost]
        public IActionResult SaveAnswer(int attemptId, int questionId, int selectedOptionId, int questionIndex)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return Unauthorized();

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null || student.CollegeId != sessionCollegeId.Value)
                return Unauthorized();

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefault(a =>
                    a.Id == attemptId &&
                    a.StudentId == student.Id &&
                    a.CollegeId == student.CollegeId
                );

            if (attempt == null || attempt.EndTime != null)
                return Unauthorized();

            var designatedEndTime = attempt.StartTime.AddMinutes(attempt.Exam.DurationInMinutes);
            var actualEndTime = attempt.Exam.EndDateTime.HasValue && attempt.Exam.EndDateTime.Value < designatedEndTime
                ? attempt.Exam.EndDateTime.Value
                : designatedEndTime;

            if (OnlineExamSystem.Helpers.TimeHelper.GetLocalTime() > actualEndTime.AddMinutes(1))
                return Unauthorized();

            // 🔐 Ensure question belongs to this exam + college
            var questionExists = _context.Questions.Any(q =>
                q.Id == questionId &&
                q.ExamId == attempt.ExamId &&
                q.CollegeId == student.CollegeId
            );

            if (!questionExists)
                return Unauthorized();

            // 🔐 Ensure option belongs to this question + college
            var optionExists = _context.Options.Any(o =>
                o.Id == selectedOptionId &&
                o.QuestionId == questionId &&
                o.CollegeId == student.CollegeId
            );

            if (!optionExists)
                return Unauthorized();

            var existingAnswer = _context.StudentAnswers
                .FirstOrDefault(sa =>
                    sa.ExamAttemptId == attemptId &&
                    sa.QuestionId == questionId &&
                    sa.CollegeId == student.CollegeId
                );

            if (existingAnswer == null)
            {
                _context.StudentAnswers.Add(new StudentAnswer
                {
                    ExamAttemptId = attemptId,
                    QuestionId = questionId,
                    SelectedOptionId = selectedOptionId,
                    CollegeId = student.CollegeId,   // 🔐 REQUIRED
                    AnsweredAt = DateTime.UtcNow
                });
            }
            else
            {
                existingAnswer.SelectedOptionId = selectedOptionId;
                existingAnswer.AnsweredAt = DateTime.UtcNow;
            }

            _context.SaveChanges();

            return Ok(new { success = true });
        }

        // -------------------------------
        // SUBMIT (FINAL)
        // -------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitAll(int attemptId, Dictionary<int, int> answers)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return Unauthorized();

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized();

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null || student.CollegeId != sessionCollegeId.Value)
                return Unauthorized();

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                .FirstOrDefault(a =>
                    a.Id == attemptId &&
                    a.StudentId == student.Id &&
                    a.CollegeId == student.CollegeId
                );

            if (attempt == null || attempt.EndTime != null)
                return Unauthorized();

            var designatedEndTime = attempt.StartTime.AddMinutes(attempt.Exam.DurationInMinutes);
            var actualEndTime = attempt.Exam.EndDateTime.HasValue && attempt.Exam.EndDateTime.Value < designatedEndTime
                ? attempt.Exam.EndDateTime.Value
                : designatedEndTime;

            if (answers == null)
                answers = new Dictionary<int, int>();

            if (OnlineExamSystem.Helpers.TimeHelper.GetLocalTime() <= actualEndTime.AddMinutes(2))
            {
                var oldAnswers = _context.StudentAnswers
                    .Where(sa =>
                        sa.ExamAttemptId == attemptId &&
                        sa.CollegeId == student.CollegeId
                    );

                _context.StudentAnswers.RemoveRange(oldAnswers);

                foreach (var entry in answers)
                {
                    // 🔐 Validate question belongs to this exam + college
                    var validQuestion = _context.Questions.Any(q =>
                        q.Id == entry.Key &&
                        q.ExamId == attempt.ExamId &&
                        q.CollegeId == student.CollegeId
                    );

                    if (!validQuestion)
                        continue;

                    // 🔐 Validate option belongs to this question + college
                    var validOption = _context.Options.Any(o =>
                        o.Id == entry.Value &&
                        o.QuestionId == entry.Key &&
                        o.CollegeId == student.CollegeId
                    );

                    if (!validOption)
                        continue;

                    _context.StudentAnswers.Add(new StudentAnswer
                    {
                        ExamAttemptId = attemptId,
                        QuestionId = entry.Key,
                        SelectedOptionId = entry.Value,
                        CollegeId = student.CollegeId,  // 🔐 REQUIRED
                        AnsweredAt = DateTime.UtcNow
                    });
                }

                _context.SaveChanges();
            }

            attempt.Score =
                (from sa in _context.StudentAnswers
                 join opt in _context.Options on sa.SelectedOptionId equals opt.Id
                 join q in _context.Questions on sa.QuestionId equals q.Id
                 where sa.ExamAttemptId == attemptId
                       && sa.CollegeId == student.CollegeId
                       && opt.IsCorrect
                 select q.Marks).Sum();

            attempt.EndTime = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime();

            var behaviorLog = GetOrCreateBehaviorLog(student.Id, attempt.ExamId);
            double totalSeconds = (attempt.EndTime.Value - attempt.StartTime).TotalSeconds;
            behaviorLog.TotalExamTime = totalSeconds / 60.0;
            int totalQuestions = attempt.Exam.Questions.Count;
            if (totalQuestions > 0)
                behaviorLog.AvgTimePerQuestion = totalSeconds / totalQuestions;

            _context.SaveChanges();

            return RedirectToAction("Index", "Exams");
        }

        // -------------------------------
        // RESULT
        // -------------------------------
        [HttpGet]
        public IActionResult Result(int attemptId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null || role == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive)
                return Unauthorized();

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(a => a.StudentAnswers)
                .Include(a => a.ExamProctorLogs) // 📸 For Proctoring Report
                .Include(a => a.Student)
                .AsNoTracking()
                .FirstOrDefault(a => a.Id == attemptId);

            if (attempt == null)
                return Unauthorized();

            // ===============================
            // STUDENT
            // ===============================
            if (role == "Student")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                var student = _context.Students
                    .AsNoTracking()
                    .FirstOrDefault(s =>
                        s.UserId == user.Id &&
                        s.CollegeId == sessionCollegeId.Value);

                if (student == null || attempt.StudentId != student.Id)
                    return Forbid();

                // Student cannot view before exam ends
                if (attempt.Exam.EndDateTime.HasValue &&
                    OnlineExamSystem.Helpers.TimeHelper.GetLocalTime() < attempt.Exam.EndDateTime.Value)
                {
                    TempData["ErrorMessage"] = "Results will be available after the exam ends.";
                    return RedirectToAction("Index", "Exams");
                }
            }

            // ===============================
            // TEACHER
            // ===============================
            else if (role == "Teacher")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                var teacher = _context.Teachers
                    .AsNoTracking()
                    .FirstOrDefault(t =>
                        t.UserId == user.Id &&
                        t.CollegeId == sessionCollegeId.Value);

                if (teacher == null ||
                    attempt.Exam.CreatedByTeacherId != teacher.Id)
                    return Forbid();
            }

            // ===============================
            // TEACHER ADMIN
            // ===============================
            else if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                // Must belong to same college
                if (attempt.CollegeId != sessionCollegeId.Value)
                    return Forbid();
            }

            // ===============================
            // ADMIN
            // ===============================
            else if (role == "Admin")
            {
                // Admin can see everything
                // No restrictions
            }

            else
            {
                return Unauthorized();
            }

            if (attempt.EndTime == null)
                return RedirectToAction("Index", "Exams");

            return View(attempt);
        }

        // -------------------------------
        // PROCTORING: LOG TAB SWITCH
        // -------------------------------
        [HttpPost]
        public IActionResult LogTabSwitch(int attemptId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return Unauthorized(new { message = "User not authenticated." });

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized(new { message = "Invalid user." });

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null || student.CollegeId != sessionCollegeId.Value)
                return Unauthorized(new { message = "Student not found or tenant mismatch." });

            var attempt = _context.ExamAttempts
                .FirstOrDefault(a =>
                    a.Id == attemptId &&
                    a.StudentId == student.Id &&
                    a.CollegeId == student.CollegeId
                );

            if (attempt == null)
                return Unauthorized(new { message = "Attempt not found or permission denied." });

            if (attempt.EndTime != null)
                return BadRequest(new { message = "Exam already submitted." });

            var behaviorLog = GetOrCreateBehaviorLog(student.Id, attempt.ExamId);
            behaviorLog.TabSwitchCount++;
            behaviorLog.ViolationCount++;
            if (behaviorLog.ViolationCount >= 3) behaviorLog.IsSuspicious = true;

            _context.SaveChanges();

            return Ok(new { success = true, tabSwitchCount = behaviorLog.TabSwitchCount });
        }

        // -------------------------------
        // PROCTORING: LOG FULL SCREEN EXIT
        // -------------------------------
        [HttpPost]
        public IActionResult LogFullScreenExit(int attemptId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return Unauthorized(new { message = "User not authenticated." });

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized(new { message = "Invalid user." });

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null || student.CollegeId != sessionCollegeId.Value)
                return Unauthorized(new { message = "Student not found or tenant mismatch." });

            var attempt = _context.ExamAttempts
                .FirstOrDefault(a =>
                    a.Id == attemptId &&
                    a.StudentId == student.Id &&
                    a.CollegeId == student.CollegeId
                );

            if (attempt == null)
                return Unauthorized(new { message = "Attempt not found or permission denied." });

            if (attempt.EndTime != null)
                return BadRequest(new { message = "Exam already submitted." });

            attempt.FullScreenExitCount++;
            
            var behaviorLog = GetOrCreateBehaviorLog(student.Id, attempt.ExamId);
            behaviorLog.FullscreenExitCount = attempt.FullScreenExitCount;
            behaviorLog.ViolationCount++;
            if (behaviorLog.ViolationCount >= 3) behaviorLog.IsSuspicious = true;

            _context.SaveChanges();

            return Ok(new { success = true, exitCount = attempt.FullScreenExitCount });
        }

        // -------------------------------
        // PROCTORING: CAPTURE IMAGE
        // -------------------------------
        [HttpPost]
        public IActionResult CaptureImage(int attemptId, [FromBody] ImageCaptureRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (userId == null || sessionCollegeId == null)
                return Unauthorized(new { message = "User not authenticated." });

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == userId.Value);

            if (user == null || !user.IsActive || user.Role != "Student")
                return Unauthorized(new { message = "Invalid user." });

            var student = _context.Students
                .AsNoTracking()
                .FirstOrDefault(s => s.UserId == user.Id);

            if (student == null || student.CollegeId != sessionCollegeId.Value)
                return Unauthorized(new { message = "Student not found or tenant mismatch." });

            var attempt = _context.ExamAttempts
                .Include(a => a.Exam)
                .FirstOrDefault(a =>
                    a.Id == attemptId &&
                    a.StudentId == student.Id &&
                    a.CollegeId == student.CollegeId
                );

            if (attempt == null)
                return Unauthorized(new { message = "Attempt not found or permission denied." });

            if (attempt.EndTime != null)
                return BadRequest(new { message = "Exam already submitted." });

            if (string.IsNullOrEmpty(request?.Base64Image))
                return BadRequest(new { message = "No image data provided." });

            try
            {
                // Remove the "data:image/jpeg;base64," part
                var base64Data = request.Base64Image.Contains(",") ? request.Base64Image.Split(',')[1] : request.Base64Image;
                var imageBytes = Convert.FromBase64String(base64Data);

                var proctoringDir = Path.Combine(_env.WebRootPath, "proctoring");
                if (!Directory.Exists(proctoringDir))
                {
                    Directory.CreateDirectory(proctoringDir);
                }

                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                var fileName = $"{attemptId}_{timestamp}.jpg";
                var filePath = Path.Combine(proctoringDir, fileName);

                System.IO.File.WriteAllBytes(filePath, imageBytes);

                var relativePath = $"/proctoring/{fileName}";

                var log = new ExamProctorLog
                {
                    ExamAttemptId = attemptId,
                    ImagePath = relativePath,
                    CapturedAt = DateTime.UtcNow,
                    SuspiciousFlag = request.SuspiciousFlag,
                    SuspiciousReason = request.SuspiciousReason
                };

                if (request.SuspiciousFlag == true)
                {
                    var behaviorLog = GetOrCreateBehaviorLog(student.Id, attempt.ExamId);
                    behaviorLog.IsSuspicious = true;
                    behaviorLog.ViolationCount++;
                }

                _context.ExamProctorLogs.Add(log);
                _context.SaveChanges();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                // Log this properly in a real app
                return StatusCode(500, new { message = "Error saving image.", error = ex.Message });
            }
        }

        private ExamBehaviorLog GetOrCreateBehaviorLog(int studentId, int examId)
        {
            var log = _context.ExamBehaviorLogs.FirstOrDefault(l => l.StudentId == studentId && l.ExamId == examId);
            if (log == null)
            {
                log = new ExamBehaviorLog
                {
                    StudentId = studentId,
                    ExamId = examId,
                    CreatedAt = OnlineExamSystem.Helpers.TimeHelper.GetLocalTime()
                };
                _context.ExamBehaviorLogs.Add(log);
                _context.SaveChanges(); // Need this to get log.Id if needed, though for saving updates later it's fine.
            }
            return log;
        }
    }

    public class ImageCaptureRequest
    {
        public string Base64Image { get; set; }
        public bool? SuspiciousFlag { get; set; }
        public string SuspiciousReason { get; set; }
    }
}