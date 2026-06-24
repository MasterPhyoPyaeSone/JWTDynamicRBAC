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
            
            return Ok(response);
        }
    }
}