using System.Text;
using JWTDynamicRBAC.Database.AppDbContextModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Services များကို Register လုပ်ခြင်း
builder.Services.AddScoped<JWTDynamicRBAC.API.Features.Auth.IAuthService, JWTDynamicRBAC.API.Features.Auth.AuthService>();
builder.Services.AddScoped<JWTDynamicRBAC.API.Features.Product.IProductService, JWTDynamicRBAC.API.Features.Product.ProductService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("http://localhost:5014") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});

// app.UseCors("AllowBlazor"); // 💡 app.UseRouting() ရဲ့ အောက်မှာ ထည့်ပေးရပါမယ်
//step 1 , DI for DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//step2 💡 .NET 8 ၏ မူလ Swagger စနစ် (JWT Token ထည့်ရန် Box အပါအဝင်)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Dynamic RBAC API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "သင့် Token ကို အောက်ပါ Box တွင် ထည့်ပါ။"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// step3 JWT Authentication စနစ်
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

//step4 Dynamic Policy မှတ်ပုံတင်ခြင်း
var serviceProvider = builder.Services.BuildServiceProvider();
using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var permissions = dbContext.Permissions.Select(p => p.PermissionName).ToList();

        builder.Services.AddAuthorization(options =>
        {
            foreach (var permission in permissions)
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("Permission", permission));
            }
        });
    }
    catch 
    { 
        builder.Services.AddAuthorization(); 
    }
}

builder.Services.AddControllers();//important
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // 💡 .NET 8 ၏ မူလ Swagger UI ကို ပြသခြင်း
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamic RBAC API v1");
    });
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowBlazor");
app.UseRouting();
// app.UseHttpsRedirection();
// Authentication က Authorization ထက် အရင်လာရပါမည်
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


