using Microsoft.AspNetCore.Mvc;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Controllers
{
    public class AdminCollegesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AdminCollegesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminColleges + SEARCH
        public IActionResult Index(string search)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var collegesQuery = _context.Colleges.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                collegesQuery = collegesQuery.Where(c =>
                    c.Name != null && c.Name.Contains(search)
                );
            }

            ViewBag.Search = search;

            var colleges = collegesQuery.ToList();
            return View(colleges);
        }

        // GET: AdminColleges/Create
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // POST: AdminColleges/Create
        [HttpPost]
        public IActionResult Create(College model)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "College name is required.");
                return View(model);
            }

            var collegeExists = _context.Colleges
                .Any(c => c.Name.ToLower() == model.Name.ToLower());

            if (collegeExists)
            {
                ModelState.AddModelError("Name", "A college with this name already exists.");
                return View(model);
            }

            model.IsActive = true;

            _context.Colleges.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "College added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminColleges/Edit/5
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var college = _context.Colleges.FirstOrDefault(c => c.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            return View(college);
        }

        // POST: AdminColleges/Edit/5
        [HttpPost]
        public IActionResult Edit(int id, College model)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var college = _context.Colleges.FirstOrDefault(c => c.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "College name is required.");
                return View(college);
            }

            var duplicateExists = _context.Colleges
                .Any(c => c.Id != id && c.Name.ToLower() == model.Name.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A college with this name already exists.");
                return View(college);
            }

            college.Name = model.Name;

            _context.SaveChanges();

            TempData["Success"] = "College updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // TOGGLE ACTIVE / INACTIVE
        public IActionResult ToggleStatus(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var college = _context.Colleges.FirstOrDefault(c => c.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            college.IsActive = !college.IsActive;

            _context.SaveChanges();

            TempData["Success"] = "College status updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: AdminColleges/Delete/5
        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var college = _context.Colleges.FirstOrDefault(c => c.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            _context.Colleges.Remove(college);
            _context.SaveChanges();

            TempData["Success"] = "College deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
