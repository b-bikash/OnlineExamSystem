using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Services.DemoData;

namespace OnlineExamSystem.Controllers
{
    public class DemoController : Controller
    {
        private readonly IDemoDataSeederService _seederService;

        public DemoController(IDemoDataSeederService seederService)
        {
            _seederService = seederService;
        }

        // =========================
        // DEMO DATA PAGE
        // =========================
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // GENERATE DEMO DATA
        // =========================
        [HttpPost]
        public async Task<IActionResult> Generate()
        {
            await _seederService.SeedAsync();

            TempData["Success"] = "Demo data generated successfully!";

            return RedirectToAction("Index");
        }
    }
}