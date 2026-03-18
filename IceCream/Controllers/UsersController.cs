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

        newUser.Role = "User";
        var created = service.Create(newUser);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
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

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public IActionResult Create(UserModel newUser)
    {
        service.Create(newUser);
        return CreatedAtAction(nameof(Get), new { id = newUser.Id }, newUser);
    }

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
        var addPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "ice_added", iceId = ice.Id });
        _ = NotificationHub.NotifyUser(hubContext, user.Id.ToString(), addPayload);

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
        var delPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "ice_deleted", iceId = iceId });
        _ = NotificationHub.NotifyUser(hubContext, user.Id.ToString(), delPayload);

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
        var updPayload = System.Text.Json.JsonSerializer.Serialize(new { action = "ice_updated", iceId = iceId });
        _ = NotificationHub.NotifyUser(hubContext, user.Id.ToString(), updPayload);

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
}
