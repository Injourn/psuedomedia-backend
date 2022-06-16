using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Services;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web.Http.Filters;

namespace PsuedoMediaBackend.Filters {
    public class PsuedoMediaAuthentication : Attribute, IAuthenticationFilter {
        public bool AllowMultiple => false;
        readonly AuthenticationService _authenticationService;        

        public PsuedoMediaAuthentication(AuthenticationService authenticationService) {
            _authenticationService = authenticationService;
        }

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken) {
            HttpRequestMessage request = context.Request;
            AuthenticationHeaderValue authorization = request.Headers.Authorization;

            if (authorization == null) {
                context.ErrorResult = new AuthenticationFailureResult("Bad Request", request);
                return;
            }

            if (authorization.Scheme != "Bearer") {
                context.ErrorResult = new AuthenticationFailureResult("Bad Request", request);
                return;
            }

            string? authToken = request.Headers.GetValues("pm-authToken").FirstOrDefault();
            string? refreshToken = request.Headers.GetValues("pm-refreshToken").FirstOrDefault();
            
            if(!await AuthenticateAsync(authToken, refreshToken)) {
                context.ErrorResult = new AuthenticationFailureResult("Invalid Login", request);
            }
        }

        public async Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken) {
        }

        private async Task<bool> AuthenticateAsync(string? authToken,string? refreshToken) {
            if (authToken == null || refreshToken == null) {
                return false;
            }
            //TODO: Add jwt expiration and userId
            OAuthToken? foundToken = (await _authenticationService.OAuthService.GetAllByDefinition(x => x.Token == authToken && x.RemoveDate < DateTime.Now)).FirstOrDefault();
            if (foundToken == null) {
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
                _authenticationService.ActiveUserId = foundToken.UserId;
            }
            return true;
        }
    }
}
