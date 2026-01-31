using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace OnlineExamSystem.Controllers
{
    public class BaseController : Controller
    {
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

            // 🔑 IMPORTANT:
            // Do NOT block POST requests here
            if (httpMethod == "POST")
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
