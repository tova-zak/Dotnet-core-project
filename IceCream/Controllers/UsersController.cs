using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using IceCream.Models;
using IceCream.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using IceCream.Services;
using Microsoft.AspNetCore.SignalR;
using IceCream.Hubs;

namespace IceCream.Controllers;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private readonly IUserService service;
    private readonly IHubContext<NotificationHub> hubContext;

    public UserController(IUserService IC, IHubContext<NotificationHub> hubContext)
    {
        this.service = IC;
        this.hubContext = hubContext;
    }

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public ActionResult<IEnumerable<UserModel>> GetAll() => service.Get();

    [HttpGet("{id}")]
    [Authorize(Policy = "AllUsers")]
    public ActionResult<UserModel> Get(int id)
    {
        var user = service.Get(id);
        if (user == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString())
                return Forbid();
        }

        return user;
    }

    [HttpPost]
    [Route("[action]")]
    [AllowAnonymous]
    public ActionResult<string> Login([FromBody] UserModel User)
    {
        if (User == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(User.ShopName) || string.IsNullOrWhiteSpace(User.Password))
            return BadRequest("username and password required");

        var storedUser = service.Get().FirstOrDefault(u => u.ShopName.Equals(User.ShopName, System.StringComparison.OrdinalIgnoreCase));
        if (storedUser == null) return Unauthorized();

        if (!PasswordHasher.Verify(User.Password, storedUser.Password)) return Unauthorized();

        bool isAdmin = storedUser.Role == "Admin";
        var claims = new List<Claim>
        {
            new Claim("userShopName", storedUser.ShopName),
            new Claim("type", isAdmin ? "Admin" : "User"),
            new Claim("userId", storedUser.Id.ToString()),
        };

        var token = UserTokenService.GetToken(claims);
        return new OkObjectResult(UserTokenService.WriteToken(token));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public ActionResult<UserModel> Register([FromBody] UserModel newUser)
    {
        if (newUser == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(newUser.ShopName) || string.IsNullOrWhiteSpace(newUser.Password))
            return BadRequest("ShopName and Password are required");

        if (service.Get().Any(u => u.ShopName.Equals(newUser.ShopName, System.StringComparison.OrdinalIgnoreCase)))
            return Conflict("ShopName already exists");

        // password strength validation
        if (!IsStrongPassword(newUser.Password, out var pwdErrors))
            return BadRequest(string.Join("; ", pwdErrors));

        // prevent reuse of same password by different users
        if (service.Get().Any(u => !string.IsNullOrEmpty(u.Password) && PasswordHasher.Verify(newUser.Password, u.Password)))
            return Conflict("Password is already used by another user. Please choose a different password.");

        newUser.Role = "User";
        var created = service.Create(newUser);

        // notify admins/clients that a new user was added
        try
        {
            var addPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "user_added", userId = created.Id, shopName = created.ShopName });
            _ = hubContext.Clients.All.SendAsync("Notify", addPayload);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[UsersController] Failed to broadcast new user: {ex}");
        }

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public IActionResult Create(UserModel newUser)
    {
        if (newUser == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(newUser.Password)) return BadRequest("Password required");

        if (!IsStrongPassword(newUser.Password, out var pwdErrors))
            return BadRequest(string.Join("; ", pwdErrors));

        if (service.Get().Any(u => !string.IsNullOrEmpty(u.Password) && PasswordHasher.Verify(newUser.Password, u.Password)))
            return Conflict("Password is already used by another user. Please choose a different password.");

        // ensure Role default if not supplied
        if (string.IsNullOrWhiteSpace(newUser.Role)) newUser.Role = "User";

        service.Create(newUser);
        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
    }

    [HttpPost]
    [Route("[action]")]
    [Authorize(Policy = "Admin")]
    public IActionResult GenerateBadge([FromBody] UserModel User)
    {
        var claims = new List<Claim>
        {
            new Claim("userShopName", User.ShopName),
            new Claim("type", "Agent"),
            new Claim("clearanceLevel", User.Id.ToString()),
        };
        var token = UserTokenService.GetToken(claims);
        return new OkObjectResult(UserTokenService.WriteToken(token));
    }

<<<<<<< HEAD
    [HttpPost]
    [Authorize(Policy = "Admin")]
    public IActionResult Create(UserModel newUser)
    {
        service.Create(newUser);

        // broadcast new user to connected clients (admins)
        try
        {
            var addPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "user_added", userId = newUser.Id, shopName = newUser.ShopName });
            _ = hubContext.Clients.All.SendAsync("Notify", addPayload);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[UsersController] Failed to broadcast new user (admin create): {ex}");
        }

        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
    }

=======
>>>>>>> e69c7888336cc8e2c0190e1c321272fbb59f5220
    [HttpPut("{id}")]
    [Authorize(Policy = "AllUsers")]
    public IActionResult Update(int id, UserModel newUser)
    {
        if (id != newUser.Id) return BadRequest();
        var existing = service.Get(id);
        if (existing == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim == "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();
        }
        else
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();
        }

        service.Update(id, newUser);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Admin")]
    public IActionResult Delete(int id)
    {
        var existing = service.Get(id);
        if (existing == null) return NotFound();

        // notify the user that they were deleted by an admin
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { action = "user_deleted", userId = existing.Id, reason = "הוסר על ידי מנהל" });
            // send directly to the user's active connections (if any)
            _ = NotificationHub.NotifyUser(hubContext, existing.Id.ToString(), payload);

            // also broadcast to all clients so admin lists refresh
            _ = hubContext.Clients.All.SendAsync("Notify", payload);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[UsersController] Failed to notify user about deletion: {ex}");
        }

        service.Delete(id);
        return NoContent();
    }

    // per-user ice creams
    [HttpGet("{id}/icecreams")]
    [Authorize(Policy = "AllUsers")]
    public ActionResult<IEnumerable<IceCreamModel>> GetUserIceCreams(int id)
    {
        var user = service.Get(id);
        if (user == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();
        }

        return Ok(user.IceCreams ?? new List<IceCreamModel>());
    }

    [HttpPost("{id}/icecreams")]
    [Authorize(Policy = "AllUsers")]
    public IActionResult AddUserIceCream(int id, [FromBody] IceCreamModel ice)
    {
        var user = service.Get(id);
        if (user == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();
        }

        var maxId = user.IceCreams != null && user.IceCreams.Any() ? user.IceCreams.Max(i => i.Id) : 0;
        ice.Id = maxId + 1;
        user.IceCreams.Add(ice);
        service.Update(id, user);

        // notify user's active connections about the addition
        var addPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "ice_added", iceId = ice.Id, userId = user.Id });
        // broadcast to all clients so admins see it
        _ = hubContext.Clients.All.SendAsync("Notify", addPayload);

        return CreatedAtAction(nameof(GetUserIceCreams), new { id = id }, ice);
    }

    [HttpDelete("{id}/icecreams/{iceId}")]
    [Authorize(Policy = "AllUsers")]
    public IActionResult DeleteUserIceCream(int id, int iceId)
    {
        var user = service.Get(id);
        if (user == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();
        }

        var ice = user.IceCreams.FirstOrDefault(i => i.Id == iceId);
        if (ice == null) return NotFound();

        user.IceCreams.Remove(ice);
        service.Update(id, user);

        // notify user's active connections about deletion
        var delPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "ice_deleted", iceId = iceId, userId = user.Id });
        // broadcast to all clients so admins see it
        _ = hubContext.Clients.All.SendAsync("Notify", delPayload);

        return NoContent();
    }

    [HttpPut("{id}/icecreams/{iceId}")]
    [Authorize(Policy = "AllUsers")]
    public IActionResult UpdateUserIceCream(int id, int iceId, [FromBody] IceCreamModel updated)
    {
        var user = service.Get(id);
        if (user == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();
        }

        var ice = user.IceCreams?.FirstOrDefault(i => i.Id == iceId);
        if (ice == null) return NotFound();

        ice.Name = updated.Name ?? ice.Name;
        ice.IsDiary = updated.IsDiary;

        service.Update(id, user);

        // notify user's active connections about update
        var updPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "ice_updated", iceId = iceId, userId = user.Id });
        // broadcast to all clients so admins see it
        _ = hubContext.Clients.All.SendAsync("Notify", updPayload);

        return NoContent();
    }

    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    [HttpPost("{id}/changepassword")]
    [Authorize(Policy = "AllUsers")]
    public ActionResult<string> ChangePassword(int id, [FromBody] ChangePasswordModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.NewPassword))
            return BadRequest("New password required");

        var user = service.Get(id);
        if (user == null) return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            if (userIdClaim == null || userIdClaim != id.ToString()) return Forbid();

            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                return BadRequest("Current password required");

            if (!PasswordHasher.Verify(model.CurrentPassword, user.Password))
                return BadRequest("Current password is incorrect");
        }

        user.Password = PasswordHasher.Hash(model.NewPassword);
        service.Update(id, user);

        return NoContent();
    }

    [HttpPost("passwordused")]
    [AllowAnonymous]
    public ActionResult<object> PasswordUsed([FromBody] PasswordCheckModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Password))
            return BadRequest(new { used = false });

        var users = service.Get();
        // check if any existing user's hashed password matches the provided plain password
        foreach (var u in users)
        {
            if (!string.IsNullOrEmpty(u.Password) && PasswordHasher.Verify(model.Password, u.Password))
                return Ok(new { used = true });
        }

        return Ok(new { used = false });
    }

    public class PasswordCheckModel
    {
        public string Password { get; set; } = string.Empty;
    }

    // helper: at least 8 chars, at least 2 letters and 2 digits
    private static bool IsStrongPassword(string pwd, out List<string> errors)
    {
        errors = new List<string>();
        if (string.IsNullOrEmpty(pwd) || pwd.Length < 8)
            errors.Add("הסיסמה חייבת להכיל לפחות 8 תווים");
        int letters = pwd.Count(char.IsLetter);
        int digits = pwd.Count(char.IsDigit);
        if (letters < 2) errors.Add("הסיסמה חייבת להכיל לפחות 2 אותיות");
        if (digits < 2) errors.Add("הסיסמה חייבת להכיל לפחות 2 ספרות");
        return errors.Count == 0;
    }
}
