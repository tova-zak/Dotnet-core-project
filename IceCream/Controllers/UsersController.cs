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
[Authorize(Policy="AllUsers")]
public class UserController : ControllerBase
{  private readonly IUserService service;
   

                      
    public UserController(IUserService IC)
    {
        this.service = IC;
    }
   

    [HttpGet()]
    public ActionResult<IEnumerable<UserModel>> GetAll() => service.Get();

    [HttpGet("{id}")]
    public ActionResult<UserModel> Get(int id)
    {
        return service.Get(id);
        
   }
        [HttpPost]
        [Route("[action]")]
        [AllowAnonymous] // מאפשר גישה ל-login גם בלי טוקן — חיוני כדי לקבל טוקן במקודם
        public ActionResult<string> Login([FromBody] UserModel User)
        {
            // בדיקה שהגוף שנשלח אינו null
            if (User == null) // if request body missing -> bad request
                return BadRequest();

            // דרישת שם משתמש וסיסמה מדויקים (הוספתי בדיקה לסיסמה)
            if (User.FirstName != "Iuser" || User.Password != "1234") // only allow this fixed user/password
            {
                return Unauthorized(); // return 401 when credentials are wrong
            }

            var claims = new List<Claim>
            {
                new Claim("userFirstName", User.FirstName),
                new Claim("type", "Admin"),
            };

            var token = UserTokenService.GetToken(claims);

            // מחזירים את הטוקן כמחרוזת (response.text() בצד לקוח יטפל בזה)
            return new OkObjectResult(UserTokenService.WriteToken(token));
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(Policy="Admin")]
        public IActionResult GenerateBadge([FromBody] UserModel User)
        {
            var claims = new List<Claim>
            {
                new Claim("userFirstName", User.FirstName),
                new Claim("type", "Agent"),
                // use Id as a simple integer-based clearance level since model has no clearanceLevel property
                new Claim("clearanceLevel", User.Id.ToString()),
            };

            var token = UserTokenService.GetToken(claims);

            return new OkObjectResult(UserTokenService.WriteToken(token));
        }
       
    [HttpPost]
    [Route("[action]")]
    public IActionResult Create(UserModel newUser){
        service.Create(newUser);
        return CreatedAtAction(nameof(Get), new { id = newUser.Id },newUser);
    }
    
    [HttpPut("{id}")]
    public IActionResult Update(int id,UserModel newUser){
        if(id!=newUser.Id)
           return BadRequest();
         var existing=service.Get(id); 
         if(existing==null)
           return NotFound();
         service.Update(id, newUser) ; 
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id){
        var user=service.Get(id);
        if(user==null)
           return NotFound();
        service.Delete(id);  
        return Content(service.Get().Count.ToString());    
    }
}
