using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using IceCream.Models;
using IceCream.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using IceCream.Services;
namespace IceCream.Controllers;


[ApiController]
[Route("user")]
public class UserController : ControllerBase
{  private readonly IUserService service;
   

                      
    public UserController(IUserService IC)
    {
        this.service = IC;
    }
   

    [HttpGet()]
    [Authorize(Policy = "Admin")]
    public ActionResult<IEnumerable<UserModel>> GetAll() => service.Get();

    [HttpGet("{id}")]
    [Authorize(Policy = "AllUsers")]
    public ActionResult<UserModel> Get(int id)
    {
        // שינו כאן: כל משתמש עם טוקן (AllUsers) יכול לקרוא
        // אך אם המשתמש אינו Admin הוא יכול לראות רק את המידע שלו
        var user = service.Get(id);
        if (user == null) return NotFound();

        // עכשיו אנו משתמשים ב־claim מסוג "userShopName" כדי לזהות את השם (שם החנות) שבטוקן
        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            // לא מנהל -> יכול רק לגשת לפרטים שלו
            if (userIdClaim == null || userIdClaim != id.ToString())
                return Forbid();
        }

        return user;
        
   }
        [HttpPost]
        [Route("[action]")]
        [AllowAnonymous] // מאפשר גישה ל-login גם בלי טוקן — חיוני כדי לקבל טוקן במקודם
        public ActionResult<string> Login([FromBody] UserModel User)
        {
            // בדיקה שהגוף שנשלח אינו null
            if (User == null) // if request body missing -> bad request
                return BadRequest();

            if (string.IsNullOrWhiteSpace(User.ShopName) || string.IsNullOrWhiteSpace(User.Password))
                return BadRequest("username and password required");

            // נסה למצוא את המשתמש במאגר
            var storedUser = service.Get().FirstOrDefault(u => u.ShopName.Equals(User.ShopName, StringComparison.OrdinalIgnoreCase));
            if (storedUser == null)
                return Unauthorized();

            // בדוק סיסמה hashed
            if (!PasswordHasher.Verify(User.Password, storedUser.Password))
                return Unauthorized();

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

    // anonymous registration endpoint for new shops (users)
    [HttpPost("register")]
    [AllowAnonymous]
    public ActionResult<UserModel> Register([FromBody] UserModel newUser)
    {
        if (newUser == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(newUser.ShopName) || string.IsNullOrWhiteSpace(newUser.Password))
            return BadRequest("ShopName and Password are required");

        // לוודא שאין חפיפה של שם החנות
        if (service.Get().Any(u => u.ShopName.Equals(newUser.ShopName, StringComparison.OrdinalIgnoreCase)))
            return Conflict("ShopName already exists");

        //force role to User
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
                // use Id as a simple integer-based clearance level since model has no clearanceLevel property
                new Claim("clearanceLevel", User.Id.ToString()),
            };

            var token = UserTokenService.GetToken(claims);

            return new OkObjectResult(UserTokenService.WriteToken(token));
        }
       
    [HttpPost]
    [Authorize(Policy = "Admin")]
    public IActionResult Create(UserModel newUser){
        service.Create(newUser);
        return CreatedAtAction(nameof(Get), new { id = newUser.Id },newUser);
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = "AllUsers")]
    public IActionResult Update(int id,UserModel newUser){
        // שינו כאן: גם עדכון משתמש ב־API נתמך לכל מי שיש לו טוקן
        // אך אם המבקש אינו מנהל, הוא יכול לעדכן רק את הפרטים של עצמו
        if(id!=newUser.Id)
           return BadRequest();
         var existing=service.Get(id); 
         if(existing==null)
           return NotFound();

        var typeClaim = User.Claims.FirstOrDefault(c => c.Type == "type")?.Value;
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        if (typeClaim != "Admin")
        {
            // לא מנהל -> חייב להיות הבעלים של החשבון
            if (userIdClaim == null || userIdClaim != id.ToString())
                return Forbid();
        }

         service.Update(id, newUser) ; 
        return NoContent();
    }

    // ------------------- ניהול אוסף גלידות פר משתמש -------------------
    // המטרה: לכל משתמש יש רשימת גלידות משלו (UserModel.IceCreams). הפעולות להלן
    // מאפשרות למשתמש רגיל לראות ולשנות רק את האוסף שלו, והמנהל יכול לראות/לשנות את כל האוספים.

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
            if (userIdClaim == null || userIdClaim != id.ToString())
                return Forbid();
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
            if (userIdClaim == null || userIdClaim != id.ToString())
                return Forbid();
        }

        var maxId = user.IceCreams != null && user.IceCreams.Any() ? user.IceCreams.Max(i => i.Id) : 0;
        ice.Id = maxId + 1;
        user.IceCreams.Add(ice);
        service.Update(id, user);

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
            if (userIdClaim == null || userIdClaim != id.ToString())
                return Forbid();
        }

        var ice = user.IceCreams.FirstOrDefault(i => i.Id == iceId);
        if (ice == null) return NotFound();

        user.IceCreams.Remove(ice);
        service.Update(id, user);

        return NoContent();
    }
}
