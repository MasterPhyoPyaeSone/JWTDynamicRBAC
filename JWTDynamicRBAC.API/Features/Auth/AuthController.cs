using JWTDynamicRBAC.API.Features;
using JWTDynamicRBAC.API.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JWTDynamicRBAC.API.Features.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var response = await _authService.LoginAsync(request);
            if (response == null) return Unauthorized("Invalid username or password.");

            // 💡 JWT token ကို HttpOnly cookie အနေနဲ့ Response မှာ set လုပ်ပေးမည်
            // SameSite=None + Secure=false (localhost development အတွက်)
            Response.Cookies.Append("authToken", response.Token, new CookieOptions
            {
                HttpOnly = true,           // JS ကနေ ဖတ်လို့မရ (XSS ကာကွယ်ရန်)
                Secure = false,            // Development မှာ HTTP သုံးနိုင်ရန် false
                SameSite = SameSiteMode.None, // Cross-origin Blazor ↔ API အတွက် None
                Expires = DateTimeOffset.UtcNow.AddHours(2),
                Path = "/"
            });

            // Token ကိုလည်း body မှာ ပါ return ပေးတယ် (client-side decode အတွက်)
            return Ok(new
            {
                Token = response.Token,
                RefreshToken = response.RefreshToken,
                Message = "Login successful"
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto request)
        {
            var response = await _authService.RefreshAsync(request);
            if (response == null) return Unauthorized("Invalid refresh token.");

            // 💡 Cookie အသစ်များ ထပ်တပ်ပေးခြင်း
            Response.Cookies.Append("authToken", response.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = false,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });

            // Refresh Token ကိုလည်း လိုအပ်ရင် Cookie ထဲ ထည့်ထားနိုင်ပါသည်
            Response.Cookies.Append("refreshToken", response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = false,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize] // 💡 လက်ရှိ Login ဝင်ထားသူ (Token ရှိသူ) မှသာ ခေါ်ခွင့်ပြုမည်
        public async Task<IActionResult> Logout()
        {
            try
            {
                // ၁။ ဝင်လာတဲ့ Token ထဲကနေ လက်ရှိ User ရဲ့ Username ကို ဆွဲထုတ်မည်
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(username))
                    return Unauthorized(new { Message = "User is not authenticated." });

                // ၂။ Service (Interface) ကို လှမ်းခေါ်ပြီး DB ထဲက Token ကို ဖျက်ခိုင်းမည်
                var result = await _authService.LogoutAsync(username);

                if (!result)
                    return BadRequest(new { Message = "User not found or logout failed." });

                // ၃။ အောင်မြင်ကြောင်း ပြန်ပို့မည်
                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}