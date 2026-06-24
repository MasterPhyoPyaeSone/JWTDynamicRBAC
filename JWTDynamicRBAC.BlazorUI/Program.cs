using Blazored.SessionStorage;
using JWTDynamicRBAC.Blazor.Auth;
using JWTDynamicRBAC.BlazorUI.Auth;
using JWTDynamicRBAC.BlazorUI.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ၁။ API အတွက် HttpClient ကို Factory ဖြင့် မှတ်ပုံတင်ခြင်း
builder.Services.AddHttpClient("API", client => 
    client.BaseAddress = new Uri("http://localhost:5191/")); // သင့် API Port ကို ပြင်ပါ

// ထို HttpClient ကို Blazor Component များမှ အလွယ်တကူ လှမ်းခေါ်နိုင်ရန် Inject လုပ်ပေးခြင်း
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));

// 💡 ၁။ Browser မှ Cookie များကို ဖတ်နိုင်ရန် ဤစာကြောင်းကို ထည့်ပါ
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "authToken";
        options.LoginPath = "/login";
        options.Cookie.HttpOnly = true;
        // 💡 HTTPS မဟုတ်ဘဲ HTTP နဲ့ စမ်းနေရင် 'Always' အစား 'None' သို့မဟုတ် 'SameAsRequest' သုံးပါ
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles(); // 💡 app.js, CSS တွေကို serve ရန် Routing မတိုင်ခင် ရှိရမည်
app.UseRouting();

// 💡 Cookie auth middleware — authToken cookie ကို ဖတ်ပြီး User ကို authenticate လုပ်မည်
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
