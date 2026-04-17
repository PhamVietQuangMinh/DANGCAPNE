using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DANGCAPNE.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class PermissionAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _permissions;

        public PermissionAuthorizeAttribute(params string[] permissions)
        {
            _permissions = permissions ?? Array.Empty<string>();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var userId = http.Session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (_permissions.Length == 0)
            {
                base.OnActionExecuting(context);
                return;
            }

            var grantedPermissions = (http.Session.GetString("Permissions") ?? string.Empty)
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!_permissions.Any(permission => grantedPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
