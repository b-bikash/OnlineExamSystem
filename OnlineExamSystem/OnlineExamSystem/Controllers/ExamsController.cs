using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineExamSystem.Controllers
{
    public class ExamsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------- HELPERS ----------------

        private bool IsTeacherOrAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Teacher" || role == "Admin";
        }

        private bool ExamExists(int id)
        {
            return _context.Exams.Any(e => e.Id == id);
        }

        // ---------------- INDEX ----------------

        public IActionResult Index()
        {
            var exams = _context.Exams.ToList();
            return View(exams);
        }

        // ---------------- CREATE ----------------

        public IActionResult Create()
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            return View();
        }

        [HttpPost]
public IActionResult Create(Exam exam)
{
    _context.Exams.Add(exam);
    _context.SaveChanges();

    return RedirectToAction("Index");
}


        // ---------------- EDIT ----------------

        public IActionResult Edit(int id)
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            return View(exam);
        }

        [HttpPost]
public IActionResult Edit(int id, Exam exam)
{
    var examFromDb = _context.Exams.Find(id);
    if (examFromDb == null)
        return NotFound();

    examFromDb.Title = exam.Title;
    examFromDb.Description = exam.Description;
    examFromDb.DurationInMinutes = exam.DurationInMinutes;
    examFromDb.TotalMarks = exam.TotalMarks;

    _context.SaveChanges();
    TempData["SuccessMessage"] = "Exam updated successfully!";

    // ✅ Redirect back to Exams list page
    return RedirectToAction("Index");
}



        // ---------------- DELETE ----------------

        public IActionResult Delete(int id)
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            return View(exam);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            var exam = _context.Exams.Find(id);
            if (exam == null)
                return NotFound();

            _context.Exams.Remove(exam);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
