using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;


namespace OnlineExamSystem.Controllers
{
    public class QuestionsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int examId, int attemptId)
        {
            var questions = _context.Questions
    .Where(q => q.ExamId == examId)
    .Include(q => q.Exam)
    .ToList();

            ViewBag.AttemptId = attemptId;

            return View(questions);
        }


        public IActionResult Edit(int id)
        {
            var question = _context.Questions.Find(id);
            return View(question);
        }
        [HttpPost]
        public IActionResult Edit(Question question)
        {
            _context.Questions.Update(question);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var question = _context.Questions.Find(id);
            return View(question);
        }
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var question = _context.Questions.Find(id);

            if (question != null)
            {
                _context.Questions.Remove(question);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            ViewBag.Exams = _context.Exams.ToList();
            return View();
        }

        [HttpPost]
public IActionResult Create(Question question)
{
    _context.Questions.Add(question);
    _context.SaveChanges();

    return RedirectToAction(
        "Index",
        new
        {
            examId = question.ExamId,
            attemptId = 0
        }
    );
}

        public IActionResult SelectAnswer(int questionId, int attemptId)
        {
            var question = _context.Questions
                .Include(q => q.Exam)
                .FirstOrDefault(q => q.Id == questionId);

            ViewBag.AttemptId = attemptId;

            return View(question);
        }
        [HttpPost]
public IActionResult SelectAnswer(int questionId, int attemptId, string selectedAnswer)
{
    var answer = new Answer
    {
        QuestionId = questionId,
        ExamAttemptId = attemptId,
        SelectedAnswer = selectedAnswer
    };

    _context.Answers.Add(answer);
    _context.SaveChanges();

    var examId = _context.Questions
        .Where(q => q.Id == questionId)
        .Select(q => q.ExamId)
        .First();

    return RedirectToAction(
        "Index",
        new { examId = examId, attemptId = attemptId }
    );
}





    }
}
