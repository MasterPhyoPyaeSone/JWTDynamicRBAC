namespace JWTDynamicRBAC.API.Features.Auth
{
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

   public class TokenRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        
        // 💡 အသစ်ထပ်ဖြည့်ရမည့် Property
        public string RefreshToken { get; set; } = string.Empty; 
        
        public string Message { get; set; } = string.Empty;
    }
}