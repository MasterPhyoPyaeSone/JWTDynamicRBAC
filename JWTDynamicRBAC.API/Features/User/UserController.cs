using JWTDynamicRBAC.API.Features.Filters;
using JWTDynamicRBAC.API.Features.User;
using JWTDynamicRBAC.Database.AppDbContextModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // ၁. GET: api/Users
    [HttpGet]
    [Permission("View_User")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.Role) // Role နာမည်ပါ ပါလာအောင် Include သုံးတယ်
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                RoleId = u.RoleId,
                RoleName = u.Role != null ? u.Role.RoleName : "No Role"
            })
            .ToListAsync();

        return Ok(users);
    }

    // ၂. GET: api/Users/5
    [HttpGet("{id}")]
    [Permission("View_User")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        return Ok(new UserDto { Id = user.Id, Username = user.Username, RoleId = user.RoleId, RoleName = user?.Role?.RoleName });
    }

    // ၃. POST: api/Users
    [HttpPost]
    [Permission("View_User")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest("Username already exists.");

        var user = new User
        {
            Username = dto.Username,
            Password = dto.Password, // သတိပြုရန်: တကယ့်လုပ်ငန်းခွင်မှာ Password ကို Hash လုပ်ရပါမည် (ဥပမာ- BCrypt)
            RoleId = dto.RoleId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto { Id = user.Id, Username = user.Username, RoleId = user.RoleId });
    }

    // ၄. PUT: api/Users/5
    [HttpPut("{id}")]
    [Permission("View_User")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Username = dto.Username;
        user.RoleId = dto.RoleId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ၅. DELETE: api/Users/5
    [HttpDelete("{id}")]
    [Permission("View_User")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}