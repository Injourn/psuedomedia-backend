using Microsoft.AspNetCore.Mvc;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;
using System.Text;

namespace PsuedoMediaBackend.Controllers {
    public class AuthController : ControllerBase {
        private readonly AuthenticationService _authenticationService;

        public AuthController(AuthenticationService authenticationService) {
            _authenticationService = authenticationService;
        }

        [HttpPost, Route("[Controller]/login")]
        public async Task<IActionResult> Login([FromBody]LoginModel body) {
            if (body == null) {
                return BadRequest("Invalid Client Request");
            }
            Users? user = await _authenticationService.LoginAttempt(body.Username, body.Password);
            if (user == null) {
                return Unauthorized("Invalid Login");
            }
            else {
                LoginResponseModel loginResponseModel = await _authenticationService.AddTokens(user);
                return Ok(loginResponseModel);
            }
        }
    }
}
