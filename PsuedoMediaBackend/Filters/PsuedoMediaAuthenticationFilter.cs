using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Services;
using System.Net.Http.Headers;
using System.Web.Http.Filters;
using System.Web.Http.Results;

namespace PsuedoMediaBackend.Filters {
    public class PsuedoMediaAuthentication : Attribute, IAsyncAuthorizationFilter {
        public bool AllowMultiple => false;
        AuthenticationService _authenticationService;        

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
            HttpRequest request = context.HttpContext.Request;
            AuthenticationHeaderValue authorization;
            bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                                 .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));
            
            if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out authorization)) {
                if (!hasAllowAnonymous) {
                    context.Result = new UnauthorizedObjectResult("Bad Request");
                }
                return;
            }

            if (authorization.Scheme != "Bearer") {
                if (!hasAllowAnonymous) {
                    context.Result = new UnauthorizedObjectResult("Bad Request");
                }
                return;
            }

            _authenticationService = request.HttpContext.RequestServices.GetService<AuthenticationService>();                        

            string? authToken = authorization.Parameter;
            request.Headers.TryGetValue("pm-refreshToken", out StringValues refreshTokenString);
            string? refreshToken = refreshTokenString; 
            
            if(!await AuthenticateAsync(context,authToken, refreshToken)) {
                context.Result = new UnauthorizedObjectResult("Invalid Login");
                return;
            }
        }

        private async Task<bool> AuthenticateAsync(AuthorizationFilterContext context,string? authToken,string? refreshToken) {
            if (authToken == null) {
                return false;
            }
            //TODO: Add jwt expiration and userId
            OAuthToken? foundToken = (await _authenticationService.OAuthService.GetAllByDefinition(x => x.Token == authToken)).FirstOrDefault();
            try {
                if (foundToken != null) {
                    Users user = _authenticationService.DecodeJwtToken(foundToken.Token);
                    _authenticationService.ActiveUserId = user.Id;
                }
                else {
                    return false;
                }
            }
            catch (SecurityTokenExpiredException e) {
                if (refreshToken == null) {
                    return false;
                }
                Tuple<OAuthToken, RefreshToken>? tuple = await _authenticationService.RefreshToken(refreshToken);
                if (tuple != null) {
                    context.HttpContext.Response.Headers.Add("pm-jwtToken", tuple.Item1.Token);
                    context.HttpContext.Response.Headers.Add("pm-refreshToken", tuple.Item2.Token);
                    _authenticationService.ActiveUserId = tuple.Item2.UserId;
                }
                else return false;

            }
            catch (Exception e) {
                return false;
            }
            return true;
        }
    }
}
