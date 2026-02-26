using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OnlineExamSystem.Models;

public class SessionValidationFilter : IActionFilter
{
    private readonly ApplicationDbContext _context;

    public SessionValidationFilter(ApplicationDbContext context)
    {
        _context = context;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var sessionUserId = context.HttpContext.Session.GetInt32("UserId");

        if (sessionUserId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var user = _context.Users
            .FirstOrDefault(u => u.Id == sessionUserId && u.IsActive);

        if (user == null)
        {
            context.HttpContext.Session.Clear();
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}