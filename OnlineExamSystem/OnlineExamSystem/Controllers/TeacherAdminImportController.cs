using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Services.ImportExport;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(SessionValidationFilter))]
    public class TeacherAdminImportController : BaseController
    {
        private readonly IImportService _importService;
        private readonly ApplicationDbContext _context;

        public TeacherAdminImportController(IImportService importService, ApplicationDbContext context)
        {
            _importService = importService;
            _context = context;
        }

        // =========================
        // IMPORT PAGE
        // =========================
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            if (role == "Admin")
            {
                ViewBag.Colleges = _context.Colleges
                    .Where(c => c.IsActive)
                    .ToList();
            }

            return View();
        }

        // =========================
        // IMPORT CSV
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file, int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            int finalCollegeId;

            // 🔐 Multi-tenant enforcement
            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                finalCollegeId = sessionCollegeId.Value;
            }
            else
            {
                if (collegeId == null || collegeId == 0)
                {
                    TempData["Error"] = "Please select a college.";
                    return RedirectToAction(nameof(Index));
                }

                finalCollegeId = collegeId.Value;
            }

            var result = await _importService.ImportCourseSubjectAsync(file, finalCollegeId);

            TempData["ImportResult"] = System.Text.Json.JsonSerializer.Serialize(result);
            return RedirectToAction(nameof(Index));
        }

        //EXPORT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(int? collegeId)
        {
            var role = HttpContext.Session.GetString("Role");
            var sessionCollegeId = HttpContext.Session.GetInt32("CollegeId");

            if (role != "Admin" && role != "TeacherAdmin")
                return RedirectToAction("Index", "Dashboard");

            int finalCollegeId;

            if (role == "TeacherAdmin")
            {
                if (sessionCollegeId == null)
                    return Unauthorized();

                finalCollegeId = sessionCollegeId.Value;
            }
            else
            {
                if (collegeId == null || collegeId == 0)
                {
                    TempData["Error"] = "Please select a college.";
                    return RedirectToAction(nameof(Index));
                }

                finalCollegeId = collegeId.Value;
            }

            var csvBytes = await GenerateExportCsv(finalCollegeId);

            return File(csvBytes, "text/csv", "CourseSubjectExport.csv");
        }

        private async Task<byte[]> GenerateExportCsv(int collegeId)
        {
            var data = await (
                from s in _context.Subjects
                where s.CollegeId == collegeId
                join cs in _context.CourseSubjects
                    on s.Id equals cs.SubjectId into subjectMappings
                from sm in subjectMappings.DefaultIfEmpty()
                join c in _context.Courses
                    on sm.CourseId equals c.Id into courseJoin
                from cj in courseJoin.DefaultIfEmpty()
                select new
                {
                    Course = cj != null ? cj.Name : "",
                    Code = s.Code,
                    Name = s.Name
                }
            ).ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Course,Subject Code,Subject Name");

            foreach (var row in data)
            {
                sb.AppendLine($"{row.Course},{row.Code},{row.Name}");
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}