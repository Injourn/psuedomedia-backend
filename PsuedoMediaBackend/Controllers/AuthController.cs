using Microsoft.AspNetCore.Mvc;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Controllers {
    public class AuthController : ControllerBase {
        private readonly AuthenticationService _authenticationService;

        public AuthController(AuthenticationService authenticationService) {
            _authenticationService = authenticationService;
        }

        [HttpPost, Route("login")]
        public async Task<IActionResult> Login(LoginModel loginModel) {
            if (loginModel == null) {
                return BadRequest("Invalid Client Request");
            }
            Users? user = await _authenticationService.LoginAttempt(loginModel.Username, loginModel.Password);
            if (user == null) {
                return Unauthorized();
            }
            else {
                LoginResponseModel loginResponseModel = await _authenticationService.AddTokens(user);
                return Ok(loginResponseModel);
            }
        }
    }
}
