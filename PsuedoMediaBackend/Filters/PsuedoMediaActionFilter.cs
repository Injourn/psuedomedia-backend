using Microsoft.AspNetCore.Mvc.Filters;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Filters {
    public class PsuedoMediaActionFilter : IActionFilter {
        AuthenticationService? _authenticationService;
        public void OnActionExecuted(ActionExecutedContext context) {
            
        }

        public void OnActionExecuting(ActionExecutingContext context) {
            object body = context?.ActionArguments["body"];
            if (bods is IValidation) {
                IValidation? validationModel = context?.ActionArguments["body"] as IValidation;
                _authenticationService = context.HttpContext.Request.HttpContext.RequestServices.GetService<AuthenticationService>();
                if (validationModel != null) {
                    validationModel.Validate(_authenticationService);
                }
            }
        }
    }
}
