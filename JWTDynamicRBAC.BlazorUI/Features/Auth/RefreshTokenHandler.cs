using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

public class RefreshTokenHandler : DelegatingHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _nav;

    public RefreshTokenHandler(IHttpClientFactory httpClientFactory, IJSRuntime jsRuntime, NavigationManager nav)
    {
        _httpClientFactory = httpClientFactory;
        _jsRuntime = jsRuntime;
        _nav = nav;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // ၁။ မူလ API ကို ပုံမှန်အတိုင်း အရင်ခေါ်ကြည့်မယ်
        var response = await base.SendAsync(request, cancellationToken);

        // ၂။ 401 Unauthorized (Token သက်တမ်းကုန်သွားပြီ) ဆိုရင်...
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Refresh API ကနေ Token အသစ် လှမ်းတောင်းမယ်
            var newToken = await TryRefreshTokenAsync();

            if (!string.IsNullOrEmpty(newToken))
            {
                // 💡 ၃။ (အရေးကြီးဆုံးအပိုင်း) 
                // Request Header ထဲက Token အဟောင်းကို ဖြုတ်ပြီး၊ Token အသစ်ကို အစားထိုး တပ်ဆင်လိုက်ပါပြီ
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                // ၄။ ကျရှုံးသွားတဲ့ API ကို Token အသစ်နဲ့ ဒုတိယအကြိမ် ပြန်ခေါ်မယ်
                response = await base.SendAsync(request, cancellationToken);
            }
            else
            {
                // Refresh လုပ်လို့မရတော့ရင် (Refresh Token ပါ ကုန်သွားရင်) Logout function ခေါ်ပြီး ဖျက်မယ်
                await _jsRuntime.InvokeVoidAsync("authFunctions.logout");
                _nav.NavigateTo("/login", forceLoad: true);
            }
        }
        return response;
    }

    private async Task<string?> TryRefreshTokenAsync()
    {
        try
        {
            Console.WriteLine("=== 🔄 TryRefreshTokenAsync Started ===");
            var token = await GetCookieAsync("authToken");
            Console.WriteLine($"-> Old Token Found: {!string.IsNullOrEmpty(token)}");
            var refreshToken = await GetCookieAsync("refreshToken");
            Console.WriteLine($"-> Refresh Token Found: {!string.IsNullOrEmpty(refreshToken)}");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
            {
                Console.WriteLine("❌ Tokens are missing in Cookie! Stopping refresh process.");
                return null;
            }



            var client = _httpClientFactory.CreateClient();
            var refreshRequest = new { Token = token, RefreshToken = refreshToken };

            var response = await client.PostAsJsonAsync("http://localhost:5191/api/Auth/refresh", refreshRequest); // 💡 သင့် API လိပ်စာအတိုင်း ပြင်ပါ

            if (response.IsSuccessStatusCode)
            {
                var authResult = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                // 💡 ၁။ API က ပြန်ပေးတဲ့ Token တွေ တကယ်ပါလာလား Terminal မှာ စစ်မယ်
                Console.WriteLine("=== Refresh API Success ===");
                Console.WriteLine($"New Token: {authResult?.Token}");
                Console.WriteLine($"New RefreshToken: {authResult?.RefreshToken}");

                if (authResult != null && !string.IsNullOrEmpty(authResult.Token))
                {
                    try
                    {
                        // 💡 ၂။ JS ကို လှမ်းခေါ်တဲ့အခါ Error တက်သလား စစ်မယ်
                        await _jsRuntime.InvokeVoidAsync("authFunctions.setSecureCookie", "authToken", authResult.Token, 10080);
                        await _jsRuntime.InvokeVoidAsync("authFunctions.setSecureCookie", "refreshToken", authResult.RefreshToken, 10080);

                        Console.WriteLine("✅ Cookie Updated Successfully!");
                        return authResult.Token;
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"❌ JS Error (Cookie ရေးရာတွင် မှားယွင်းနေပါသည်): {jsEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ Refresh API Failed: {response.StatusCode}");
            }
            return null;
        }
        catch (Exception ex)
        {
            // 💡 ၃။ အခြား Error တစ်ခုခု တက်သလား စစ်မယ်
            Console.WriteLine($"❌ General Error in TryRefreshTokenAsync: {ex.Message}");
            return null;
        }
    }

    // Helper Method: JS သုံးပြီး Cookie ထဲက တန်ဖိုးကို ဖတ်ရန်
    private async Task<string> GetCookieAsync(string name)
    {
        try
        {
            var jsCode = $"document.cookie.split('; ').find(row => row.startsWith('{name}='))?.substring({name.Length + 1})";

            var result = await _jsRuntime.InvokeAsync<string>("eval", jsCode);
            return result ?? string.Empty;
        }
        catch (Exception ex)
        {
            // 💡 Cookie ဖတ်တဲ့အချိန်မှာ ဘာ Error တက်လဲဆိုတာ အတိအကျ ကြည့်ပါမယ်
            Console.WriteLine($"❌ GetCookieAsync Error ({name}): {ex.Message}");
            return string.Empty;
        }
    }
}

// Backend က ပြန်ပို့တဲ့ DTO နဲ့ တူညီရပါမည်
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}