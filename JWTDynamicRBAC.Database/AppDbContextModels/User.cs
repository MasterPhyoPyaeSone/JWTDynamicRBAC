using System;
using System.Collections.Generic;

namespace JWTDynamicRBAC.Database.AppDbContextModels;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int? RoleId { get; set; }

    public virtual Role? Role { get; set; }

    // 💡 Refresh Token အတွက် လိုအပ်သော Column များ
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
