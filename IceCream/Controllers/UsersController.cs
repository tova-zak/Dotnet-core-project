using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
 using IceCream.Models;
using IceCream.Intrfaces;
namespace IceCream.Controllers;


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
