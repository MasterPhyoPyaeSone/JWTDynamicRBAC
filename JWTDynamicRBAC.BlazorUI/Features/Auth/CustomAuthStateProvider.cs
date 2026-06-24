using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace JWTDynamicRBAC.BlazorUI.Auth
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomAuthStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // CustomAuthStateProvider.cs
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["authToken"];

            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }

            // 💡 အရေးကြီး: AuthenticationType ကို "Cookies" ဟု သတ်မှတ်မှသာ [Authorize] က အလုပ်လုပ်ပါမည်
            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), CookieAuthenticationDefaults.AuthenticationScheme);

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }

        public void NotifyUserAuthentication(string token)
        {
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(authenticatedUser)));
        }

        public void NotifyUserLogout()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            foreach (var kvp in keyValuePairs!)
            {
                // 💡 ဤနေရာတွင် JsonElement အဖြစ် ပြောင်း၍ Array ဟုတ်/မဟုတ် စစ်ဆေးပါမည်
                if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    // Admin ကဲ့သို့ Permission အများကြီး (Array) ပါလာလျှင် တစ်ခုချင်းစီ ခွဲထုတ်၍ Claim ထဲသို့ ထည့်မည်
                    foreach (var item in element.EnumerateArray())
                    {
                        claims.Add(new Claim(kvp.Key, item.ToString()));
                    }
                }
                else
                {
                    // Staff ကဲ့သို့ Permission တစ်ခုတည်း (String) ပါလာလျှင် ပုံမှန်အတိုင်း ထည့်မည်
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
                }
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4) { case 2: base64 += "=="; break; case 3: base64 += "="; break; }
            return Convert.FromBase64String(base64);
        }
    }
}