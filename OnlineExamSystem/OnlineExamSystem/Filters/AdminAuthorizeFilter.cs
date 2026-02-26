using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OnlineExamSystem.Models;

public class AdminAuthorizeFilter : IActionFilter
{
    private readonly ApplicationDbContext _context;

    public AdminAuthorizeFilter(ApplicationDbContext context)
    {
        _context = context;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        var role = session.GetString("Role");
        var userId = session.GetInt32("UserId");

        if (userId == null || string.IsNullOrEmpty(role))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (role != "Admin" && role != "TeacherAdmin")
        {
            context.Result = new RedirectToActionResult("Index", "Dashboard", null);
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}