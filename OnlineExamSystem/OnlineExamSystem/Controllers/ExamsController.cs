using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;
using System.Linq;

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
        [ValidateAntiForgeryToken]
        public IActionResult Create(Exam exam)
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            if (!ModelState.IsValid)
                return View(exam);

            _context.Exams.Add(exam);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
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
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Exam exam)
        {
            if (!IsTeacherOrAdmin())
                return Unauthorized();

            if (id != exam.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(exam);

            _context.Exams.Update(exam);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
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
