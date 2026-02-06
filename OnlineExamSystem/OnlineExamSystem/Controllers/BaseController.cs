using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace OnlineExamSystem.Controllers
{
    public class BaseController : Controller
    {
        protected string? Role =>
            HttpContext.Session.GetString("Role");

        protected int? UserId =>
            HttpContext.Session.GetInt32("UserId");

        protected bool IsAdmin => Role == "Admin";
        protected bool IsTeacher => Role == "Teacher";
        protected bool IsStudent => Role == "Student";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var httpMethod = context.HttpContext.Request.Method;

            // Allow AccountController freely
            if (controllerName == "Account")
            {
                base.OnActionExecuting(context);
                return;
            }

            // Do NOT block POST requests
            if (httpMethod == "POST")
            {
                base.OnActionExecuting(context);
                return;
            }

            // Authentication check
            if (UserId == null)
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null
                );
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
