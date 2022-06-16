using PsuedoMediaBackend.Models;
using System.Security.Cryptography;
using System.Text;

namespace PsuedoMediaBackend.Services {
    public class AuthenticationService {
        public MongoDbService<OAuthToken> OAuthService { get; private set; }
        public MongoDbService<RefreshToken> RefreshTokenService { get; private set; }

        public string? ActiveUserId { get; set; }

        public AuthenticationService() {
            OAuthService = new MongoDbService<OAuthToken>();
            RefreshTokenService = new MongoDbService<RefreshToken>();
        }

        public async Task RefreshToken(RefreshToken refreshToken) {
            OAuthToken newToken = new OAuthToken() {
                Token = GenerateRandomString(32),
                UserId = refreshToken.UserId
            };
            RefreshToken newRefreshToken = new RefreshToken() {
                UserId = refreshToken.UserId,
                Token = GenerateRandomString(32),
                IsInactive = false
            };
            refreshToken.IsInactive = true;
            await RefreshTokenService.CreateAsync(newRefreshToken);
            await OAuthService.CreateAsync(newToken);
            await RefreshTokenService.UpdateAsync(refreshToken.Id, refreshToken);
        }

        private string GenerateRandomString(int length) {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();

            while (length-- > 0) {
                byte[] nextByte = RandomNumberGenerator.GetBytes(1);
                uint num = BitConverter.ToUInt32(nextByte, 0);
                res.Append(valid[(int)(num % (uint)valid.Length)]);
            }

            return res.ToString();
        }
    }
}
