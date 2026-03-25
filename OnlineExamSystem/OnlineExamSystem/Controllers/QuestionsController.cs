using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(SessionValidationFilter))]
    public class QuestionsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public QuestionsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // -------------------------------
        // FAIRNESS CHECK (CENTRAL)
        // -------------------------------
        private bool IsQuestionModificationLocked(Exam exam)
        {
            var now = DateTime.Now;

            bool hasAttempts = _context.ExamAttempts
                .Any(ea => ea.ExamId == exam.Id);

            bool isLive =
                exam.StartDateTime.HasValue &&
                exam.EndDateTime.HasValue &&
                now >= exam.StartDateTime.Value &&
                now <= exam.EndDateTime.Value;

            return hasAttempts || isLive;
        }

        private bool HasExamAccess(Exam exam)
        {
            var role = HttpContext.Session.GetString("Role");
            var collegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role == "Admin")
                return true;

            if (collegeId == null)
                return false;

            if (role == "Teacher" || role == "TeacherAdmin")
                return exam.CollegeId == collegeId.Value;

            return false;
        }
        private void RecalculateExamTotalMarks(int examId)
        {
            var totalMarks = _context.Questions
                .Where(q => q.ExamId == examId)
                .Sum(q => q.Marks);

            var exam = _context.Exams.Find(examId);
            if (exam == null)
                return;

            exam.TotalMarks = totalMarks;
            _context.SaveChanges();
        }

        // -------------------------------
        // LIST (MANAGE QUESTIONS)
        // -------------------------------
        public IActionResult Index(int examId)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId");
            var role = HttpContext.Session.GetString("Role");

            if (role != "Teacher" && role != "TeacherAdmin" && role != "Admin")
                return Unauthorized();

            var exam = _context.Exams
                .AsNoTracking()
                .FirstOrDefault(e =>
                    e.Id == examId &&
                    (role == "Admin" || (collegeId != null && e.CollegeId == collegeId.Value))
                );

            if (exam == null)
                return NotFound();

            var questions = _context.Questions
                .Where(q => q.ExamId == examId)
                .Include(q => q.Options)
                .ToList();

            /*ViewBag.Breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Text = "Dashboard", Url = Url.Action("Index", "Dashboard") },
                new BreadcrumbItem { Text = "Exams", Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = exam.Title, Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = "Questions", IsActive = true }
            };*/

            ViewBag.ExamId = examId;
            return View(questions);
        }

        // -------------------------------
        // CREATE (GET)
        // -------------------------------
        public IActionResult Create(int examId)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId"); 
            var exam = _context.Exams.FirstOrDefault(e => e.Id == examId);
            if (exam == null)
                return NotFound();

            if (IsQuestionModificationLocked(exam))
            {
                TempData["ErrorMessage"] =
                    "Questions cannot be added once the exam is live or has been attempted.";
                return RedirectToAction("Index", new { examId });
            }

            ViewBag.Breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Text = "Dashboard", Url = Url.Action("Index", "Dashboard") },
                new BreadcrumbItem { Text = "Exams", Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = exam.Title, Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = "Questions", Url = Url.Action("Index", "Questions", new { examId }) },
                new BreadcrumbItem { Text = "Create Question", IsActive = true }
            };

            return View(new QuestionCreateViewModel { ExamId = examId, Marks = 1 });
        }

        // -------------------------------
        // CREATE (POST)
        // -------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(QuestionCreateViewModel model)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId"); 
            var exam = _context.Exams.FirstOrDefault(e => e.Id == model.ExamId);
            if (exam == null)
                return NotFound();

            if (IsQuestionModificationLocked(exam))
            {
                TempData["ErrorMessage"] =
                    "Questions cannot be added once the exam is live or has been attempted.";
                return RedirectToAction("Index", new { examId = model.ExamId });
            }

            if (!ModelState.IsValid)
                return View(model);

            var filteredOptions = model.Options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .ToList();

            if (filteredOptions.Count < 2 || filteredOptions.Count > 4)
            {
                ModelState.AddModelError("", "You must provide between 2 and 4 options.");
                return View(model);
            }

            // check for duplicates
            var distinctOptions = filteredOptions.Select(o => o.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (distinctOptions.Count != filteredOptions.Count)
            {
                ModelState.AddModelError("", "Duplicate options are not allowed.");
                return View(model);
            }

            var question = new Question
            {
                Text = model.QuestionText,
                ExamId = model.ExamId,
                Marks = model.Marks,
                CollegeId = exam.CollegeId
            };

            // handle image upload
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "questions");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(fileStream);
                }
                question.ImageUrl = uniqueFileName;
            }

            _context.Questions.Add(question);
            _context.SaveChanges();

            for (int i = 0; i < filteredOptions.Count; i++)
            {
                _context.Options.Add(new Option
                {
                    QuestionId = question.Id,
                    CollegeId = exam.CollegeId,
                    Text = filteredOptions[i],
                    IsCorrect = (i == model.CorrectOptionIndex.Value)
                });
            }

            _context.SaveChanges();
            RecalculateExamTotalMarks(model.ExamId);
            TempData["SuccessMessage"] = "Question added successfully.";
            return RedirectToAction("Index", new { examId = model.ExamId });
        }

        // -------------------------------
        // EDIT (GET)
        // -------------------------------
        public IActionResult Edit(int id)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId"); 
            var question = _context.Questions
                .Include(q => q.Options)
                .FirstOrDefault(q => q.Id == id);

            if (question == null)
                return NotFound();

            var exam = _context.Exams.AsNoTracking().FirstOrDefault(e => e.Id == question.ExamId);
            if (exam == null)
                return NotFound();

            ViewBag.Breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Text = "Dashboard", Url = Url.Action("Index", "Dashboard") },
                new BreadcrumbItem { Text = "Exams", Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = exam.Title, Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = "Questions", Url = Url.Action("Index", "Questions", new { examId = exam.Id }) },
                new BreadcrumbItem { Text = "Edit Question", IsActive = true }
            };

            var model = new QuestionEditViewModel
            {
                QuestionId = question.Id,
                ExamId = question.ExamId,
                QuestionText = question.Text,
                Marks = question.Marks,
                Options = question.Options.Select(o => new OptionEditItemViewModel
                {
                    OptionId = o.Id,
                    Text = o.Text
                }).ToList(),
                CorrectOptionId = question.Options.FirstOrDefault(o => o.IsCorrect)?.Id ?? 0,
                ExistingImageUrl = question.ImageUrl // Set existing image URL
            };

            return View(model);
        }

        // -------------------------------
        // EDIT (POST)
        // -------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(QuestionEditViewModel model)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId");
            
            // 🔴 IMPORTANT: Clear old validation state
            ModelState.Clear();

            // Remove empty option rows
            model.Options = model.Options
                .Where(o => !string.IsNullOrWhiteSpace(o.Text))
                .ToList();

            if (model.Options.Count < 2 || model.Options.Count > 4)
            {
                ModelState.AddModelError("", "You must provide between 2 and 4 options.");
            }

            // check for duplicates
            var optionTexts = model.Options.Select(o => o.Text.Trim()).ToList();
            var distinctOptionTexts = optionTexts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (optionTexts.Count != distinctOptionTexts.Count)
            {
                ModelState.AddModelError("", "Duplicate options are not allowed.");
            }

            // Re-run validation AFTER cleanup
            TryValidateModel(model);

            if (!ModelState.IsValid)
                return View(model);

            var question = _context.Questions
                .Include(q => q.Options)
                .FirstOrDefault(q => q.Id == model.QuestionId);

            if (question == null)
                return NotFound();

            // Fairness rule
            bool examAttempted = _context.ExamAttempts
                .Any(ea => ea.ExamId == question.ExamId);

            if (examAttempted)
            {
                TempData["ErrorMessage"] =
                    "This exam has already been attempted. Questions cannot be modified.";
                return RedirectToAction("Index", new { examId = question.ExamId });
            }

            // Update question
            question.Text = model.QuestionText;
            question.Marks = model.Marks;

            // handle image upload
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "questions");
                Directory.CreateDirectory(uploadsFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(fileStream);
                }

                // delete old image
                if (!string.IsNullOrEmpty(question.ImageUrl))
                {
                    string oldFilePath = Path.Combine(uploadsFolder, question.ImageUrl);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                question.ImageUrl = uniqueFileName;
            }

            // Reset correct flags
            foreach (var opt in question.Options)
                opt.IsCorrect = false;

            // Update / add options
            foreach (var optVm in model.Options)
            {
                if (optVm.OptionId == 0)
                {
                    question.Options.Add(new Option
                    {
                        QuestionId = question.Id,
                        CollegeId = question.CollegeId,
                        Text = optVm.Text,
                        IsCorrect = false
                    });
                }
                else
                {
                    var existing = question.Options
                        .FirstOrDefault(o => o.Id == optVm.OptionId);

                    if (existing != null)
                        existing.Text = optVm.Text;
                }
            }

            // Set correct option
            var correct = question.Options
                .FirstOrDefault(o => o.Id == model.CorrectOptionId);

            if (correct != null)
                correct.IsCorrect = true;

            _context.SaveChanges();
            RecalculateExamTotalMarks(question.ExamId);

            TempData["SuccessMessage"] = "Question updated successfully.";

            return RedirectToAction("Index", new { examId = question.ExamId });
        }


        // -------------------------------
        // DELETE (GET)
        // -------------------------------
        public IActionResult Delete(int id)
        {
            var question = _context.Questions
                .Include(q => q.Options)
                .FirstOrDefault(q => q.Id == id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        // -------------------------------
        // DELETE (POST)
        // -------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId"); 
            var question = _context.Questions.Include(q => q.Options).FirstOrDefault(q => q.Id == id);
            if (question == null)
                return NotFound();

            var exam = _context.Exams.FirstOrDefault(e => e.Id == question.ExamId);

            if (exam == null || !HasExamAccess(exam))
                return Unauthorized(); 
            
            var examId = question.ExamId;

            // 🔒 FAIRNESS RULE ENFORCEMENT
            bool examAttempted = _context.ExamAttempts.Any(ea => ea.ExamId == examId);

            if (examAttempted)
            {
                TempData["ErrorMessage"] =
                    "This exam has already been attempted by students. Questions cannot be deleted.";

                return RedirectToAction("Index", new { examId });
            }

            _context.Options.RemoveRange(question.Options);
            _context.Questions.Remove(question);
            _context.SaveChanges();
            RecalculateExamTotalMarks(examId);

            TempData["SuccessMessage"] = "Question deleted successfully.";
            return RedirectToAction("Index", new { examId });
        }
    }
}
