using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OnlineExamSystem.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();

            // Allow AccountController (Login / Logout)
            if (controllerName == "Account")
            {
                base.OnActionExecuting(context);
                return;
            }

            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null)
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
