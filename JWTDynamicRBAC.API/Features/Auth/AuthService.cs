using JWTDynamicRBAC.API.Features;
using JWTDynamicRBAC.API.Features.User;
using JWTDynamicRBAC.Database.AppDbContextModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JWTDynamicRBAC.API.Features.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto request);
        Task<AuthResponseDto?> RefreshAsync(TokenRequestDto request);
        Task<bool> LogoutAsync(string username);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null) return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            foreach (var permission in user.Role.Permissions)
            {
                claims.Add(new Claim("Permission", permission.PermissionName));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(1),
                signingCredentials: creds
            );

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(30);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Message = "Login Successful"
            };
        }
        public async Task<bool> LogoutAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return false; 

            // ၂။ Database ထဲက Refresh Token ကို အလွတ် (null) ပြောင်းပြီး ဖျက်ပစ်မည်
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow; // သက်တမ်းကိုပါ ကုန်ဆုံးစေမည်

            // ၃။ ပြောင်းလဲမှုများကို Database သို့ Save မည်
            await _context.SaveChangesAsync();

            return true;
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<AuthResponseDto?> RefreshAsync(TokenRequestDto request)
        {
            // ၁။ Token အဟောင်းကို Validation မလုပ်ဘဲ စစ်ဆေးခြင်း
            var principal = GetPrincipalFromExpiredToken(request.Token);
            if (principal == null) return null;

            // 💡 ပြင်ဆင်ချက် (၁) - Audience ပွားခြင်းကို ကာကွယ်ရန် System Claims များကို ဖယ်ရှားခြင်း
            // Token အဟောင်းထဲက Claims တွေကို ယူတဲ့အခါ aud, iss, exp စတာတွေကို ဖယ်ထုတ်ပြီးမှ ယူပါမယ်။
            // (အောက်က JwtSecurityToken က အဲဒီကောင်တွေကို အလိုအလျောက် အသစ်ပြန်ထည့်ပေးမှာမို့လို့ပါ)
            var claims = principal.Claims.Where(c =>
                c.Type != System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Aud &&
                c.Type != System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iss &&
                c.Type != System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp &&
                c.Type != System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat &&
                c.Type != System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Nbf
            ).ToList();

            // ၃။ Database ထဲက Refresh Token နဲ့ တိုက်စစ်ခြင်း (Security Check)
            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return null;

            // ၄။ Token အသစ် ထုတ်ပေးခြင်း
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var newToken = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims, // 👈 သန့်စင်ပြီးသား Claims များကိုသာ သုံးပါမည်
                expires: DateTime.UtcNow.AddMinutes(15), // 💡 ပြင်ဆင်ချက် (၂) - Access Token သက်တမ်းကို (၁၅) မိနစ်သို့ ပြောင်းပါ
                signingCredentials: creds
            );

            // ၅။ Refresh Token အသစ် ထုတ်ပြီး DB မှာ သိမ်းခြင်း
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;

            // 💡 အကြံပြုချက်: Refresh Token ရဲ့ သက်တမ်းကို ရက်ရှည် (ဥပမာ - ၇ ရက်) ထားပါ
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(newToken),
                RefreshToken = newRefreshToken,
                Message = "Token Refreshed"
            };
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // သင့် Config အတိုင်းပြင်ပါ
                ValidateIssuer = false,   // သင့် Config အတိုင်းပြင်ပါ
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateLifetime = false // 💡 အရေးကြီး: သက်တမ်းကုန်နေတာကို သိလို့ false ပေးရပါမယ်
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}