using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Services.ImportExport;
using Microsoft.AspNetCore.Http;

namespace OnlineExamSystem.Controllers
{
    [ServiceFilter(typeof(SessionValidationFilter))]
    public class QuestionImportController : BaseController
    {
        private readonly IQuestionImportService _importService;

        public QuestionImportController(IQuestionImportService importService)
        {
            _importService = importService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file, int examId)
        {
            var collegeId = HttpContext.Session.GetInt32("CollegeId");

            if (collegeId == null)
                return Unauthorized();

            var result = await _importService.ImportQuestionsAsync(file, examId, collegeId.Value);

            if (result.Errors.Any())
            {
                TempData["ErrorMessage"] = string.Join(" | ", result.Errors);
            }
            else
            {
                TempData["SuccessMessage"] =
                    $"Imported {result.InsertedQuestions} questions successfully.";
            }

            return RedirectToAction("Index", "Questions", new { examId });
        }
    }
}