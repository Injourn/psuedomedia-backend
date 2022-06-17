using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PsuedoMediaBackend.Services {
    public class AuthenticationService {
        const string jwtKey = "eziOmBOd6wpRktBEkambDC";
        const string issuer = "psuedoMediaExample";
        const string audience = "audience";
        public MongoDbService<OAuthToken> OAuthService { get; private set; }
        public MongoDbService<RefreshToken> RefreshTokenService { get; private set; }

        public MongoDbService<Users> UserService { get; private set; }

        public string? ActiveUserId { get; set; }

        public AuthenticationService() {
            OAuthService = new MongoDbService<OAuthToken>();
            RefreshTokenService = new MongoDbService<RefreshToken>();
            UserService = new MongoDbService<Users>();
        }

        public async Task<LoginResponseModel> AddTokens(Users user) {
            OAuthToken oAuthToken = GenerateOAuthToken(user);
            RefreshToken refreshToken = GenerateRefreshToken(user);

            await OAuthService.CreateAsync(oAuthToken);
            await RefreshTokenService.CreateAsync(refreshToken);

            return new LoginResponseModel() {
                OAuthToken = oAuthToken.Token,
                RefreshToken = refreshToken.Token,
            };
        }

        public async Task RefreshToken(RefreshToken refreshToken) {
            Users users = await UserService.GetByIdAsync(refreshToken.UserId);
            OAuthToken newToken = GenerateOAuthToken(users);
            RefreshToken newRefreshToken = GenerateRefreshToken(users);
            refreshToken.IsInactive = true;
            await RefreshTokenService.CreateAsync(newRefreshToken);
            await OAuthService.CreateAsync(newToken);
            await RefreshTokenService.UpdateAsync(refreshToken.Id, refreshToken);
        }

        public async Task<Users?> LoginAttempt(string username, string password) {
            List<Users> users = await UserService.GetAllByDefinition(x => x.Username == username && x.Password == password);
            return users.FirstOrDefault();
        }

        public OAuthToken GenerateOAuthToken(Users user) {
            return new OAuthToken() {
                Token = GenerateJwtToken(user),
            };
        }

        public RefreshToken GenerateRefreshToken(Users user) {
            return new RefreshToken() {
                Token = GenerateRandomString(32),
                UserId = user.Id,
                IsInactive = false
            };
        }

        public Users DecodeJwtToken(string token) {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jsonToken = tokenHandler.ReadJwtToken(token);
            return new Users() {
                Id = jsonToken.Claims.First(x => x.Type == "id").Value,
                DisplayName = jsonToken.Claims.First(x => x.Type == "username").Value
            };
        }

        private string GenerateRandomString(int length) {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();

            while (length-- > 0) {
                byte[] nextByte = RandomNumberGenerator.GetBytes(4);
                uint num = BitConverter.ToUInt32(nextByte, 0);
                res.Append(valid[(int)(num % (uint)valid.Length)]);
            }

            return res.ToString();
        }
        private string GenerateJwtToken(Users user) {
            var tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(jwtKey);
            Claim[] claims = new[] { 
                new Claim("id", user.Id),
                new Claim("username", user.DisplayName)
            };
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
