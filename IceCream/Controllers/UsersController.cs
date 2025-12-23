using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
 using IceCream.Models;
using IceCream.Intrfaces;
namespace IceCream.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using IceCream.Services;

[ApiController]
[Route("user")]
public class UserController : ControllerBase
{  IUserService service;
   

                      
    public UserController(IUserService IC)
    {
        this.service = IC;
    }
   

    [HttpGet()]
    public ActionResult<IEnumerable<UserModel>> Get()
    {
        return service.Get();
        
    }

    [HttpGet("{id}")]
    public ActionResult<UserModel> Get(int id)
    {
        return service.Get(id);
        
   }
        [HttpPost]
        [Route("[action]")]
        public ActionResult<String> Login([FromBody] UserModel User)
        {
            var dt = DateTime.Now;
            //var query = $"select * from users where idnumber = @idnumber";
            if (User.FirstName != "Wray"
            || User.Password != $"W{dt.Year}#{dt.Day}!")
            {
                return Unauthorized();
            }

            var claims = new List<Claim>
            {
                new Claim("FirstName", User.FirstName),
                new Claim("type", "Admin"),
            };

            var token = UserTokenService.GetToken(claims);

            return new OkObjectResult(UserTokenService.WriteToken(token));
        }

    [HttpPost]
    [Route("[action]")]
    public ActionResult Create(UserModel newUser){
        var postedUser = service.Create(newUser);
        return CreatedAtAction(nameof(Create), new { id = postedUser.Id });
    }
    
    [HttpPut("{id}")]
    public ActionResult Update(int id,UserModel newUser){
        var user=service.Update(id,newUser);
        if(!user)
        return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id){
        var user=service.Delete(id);
        if(!user)
           return NotFound();
        return NoContent();    
    }
}
