using JWTDynamicRBAC.API.Features;
using JWTDynamicRBAC.API.Features.Auth;
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
            return Ok(new { Token = response.Token, Message = "Login successful" });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("authToken");
            return Ok(new { Message = "Logged out" });
        }
    }
}