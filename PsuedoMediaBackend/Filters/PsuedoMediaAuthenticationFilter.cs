using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
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
            if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out authorization)) {
                context.Result = new UnauthorizedObjectResult("Bad Request");
                return;
            }

            if (authorization.Scheme != "Bearer") {
                context.Result = new UnauthorizedObjectResult("Bad Request");
                return;
            }

            _authenticationService = request.HttpContext.RequestServices.GetService<AuthenticationService>();                        

            string? authToken = authorization.Parameter;
            request.Headers.TryGetValue("pm-refreshToken", out StringValues refreshTokenString);
            string? refreshToken = refreshTokenString; 
            
            if(!await AuthenticateAsync(authToken, refreshToken)) {
                context.Result = new UnauthorizedObjectResult("Invalid Login");
                return;
            }
        }

        private async Task<bool> AuthenticateAsync(string? authToken,string? refreshToken) {
            if (authToken == null) {
                return false;
            }
            //TODO: Add jwt expiration and userId
            OAuthToken? foundToken = (await _authenticationService.OAuthService.GetAllByDefinition(x => x.Token == authToken)).FirstOrDefault();
            if (foundToken == null) {
                if(refreshToken == null) {
                    return false;
                }
                RefreshToken? foundRefreshToken = (await _authenticationService.RefreshTokenService.GetAllByDefinition(x => x.Token == refreshToken && x.RemoveDate < DateTime.Now)).FirstOrDefault();
                if (foundRefreshToken == null) {
                    return false;
                }
                else {
                    await _authenticationService.RefreshToken(foundRefreshToken);
                    _authenticationService.ActiveUserId = foundRefreshToken.UserId;
                }
            }
            else {
                Users user = _authenticationService.DecodeJwtToken(foundToken.Token);
                _authenticationService.ActiveUserId = user.Id;
            }
            return true;
        }
    }
}
