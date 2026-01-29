using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Linq;
using System.Collections.Generic;

namespace OnlineExamSystem.Controllers
{
    public class QuestionsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
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
            var exam = _context.Exams
                .AsNoTracking()
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null)
                return NotFound();

            var questions = _context.Questions
                .Where(q => q.ExamId == examId)
                .Include(q => q.Options)
                .ToList();

            ViewBag.Breadcrumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Text = "Dashboard", Url = Url.Action("Index", "Dashboard") },
                new BreadcrumbItem { Text = "Exams", Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = exam.Title, Url = Url.Action("Index", "Exams") },
                new BreadcrumbItem { Text = "Questions", IsActive = true }
            };

            ViewBag.ExamId = examId;
            return View(questions);
        }

        // -------------------------------
        // CREATE (GET)
        // -------------------------------
        public IActionResult Create(int examId)
        {
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

            return View(new QuestionCreateViewModel { ExamId = examId });
        }

        // -------------------------------
        // CREATE (POST)
        // -------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(QuestionCreateViewModel model)
        {
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

            var question = new Question
            {
                Text = model.QuestionText,
                ExamId = model.ExamId,
                Marks = model.Marks
            };

            _context.Questions.Add(question);
            _context.SaveChanges();

            for (int i = 0; i < filteredOptions.Count; i++)
            {
                _context.Options.Add(new Option
                {
                    QuestionId = question.Id,
                    Text = filteredOptions[i],
                    IsCorrect = (i == model.CorrectOptionIndex)
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
                CorrectOptionId = question.Options.Single(o => o.IsCorrect).Id
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
            // 🔴 IMPORTANT: Clear old validation state
            ModelState.Clear();

            // Remove empty option rows
            model.Options = model.Options
                .Where(o => !string.IsNullOrWhiteSpace(o.Text))
                .ToList();

            // Enforce 2–4 options rule
            if (model.Options.Count < 2 || model.Options.Count > 4)
            {
                ModelState.AddModelError("", "You must provide between 2 and 4 options.");
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
            var question = _context.Questions.FirstOrDefault(q => q.Id == id);
            if (question == null)
                return NotFound();

            var examId = question.ExamId;

            // 🔒 FAIRNESS RULE ENFORCEMENT
            bool examAttempted = _context.ExamAttempts.Any(ea => ea.ExamId == examId);

            if (examAttempted)
            {
                TempData["ErrorMessage"] =
                    "This exam has already been attempted by students. Questions cannot be deleted.";

                return RedirectToAction("Index", new { examId });
            }

            _context.Questions.Remove(question);
            _context.SaveChanges();
            RecalculateExamTotalMarks(examId);

            TempData["SuccessMessage"] = "Question deleted successfully.";
            return RedirectToAction("Index", new { examId });
        }
    }
}
