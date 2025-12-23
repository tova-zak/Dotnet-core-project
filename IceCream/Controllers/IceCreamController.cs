// using System.Security.Cryptography.X509Certificates;
// using Microsoft.AspNetCore.Http.HttpResults;
// using Microsoft.AspNetCore.Mvc;
// using IceCream.Services;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
 using IceCream.Models;
using IceCream.Intrfaces;
namespace IceCream.Controllers;


[ApiController]
[Route("[controller]")]
public class IceCreamController : ControllerBase
{  IOrderService service;
   

                      
    public IceCreamController(IOrderService IC)
    {
        this.service = IC;
    }
   

    [HttpGet()]
    public ActionResult<IEnumerable<IceCreamModel>> Get()
    {
        return service.Get();
        
    }

    [HttpGet("{id}")]
    public ActionResult<IceCreamModel> Get(int id)
    {
        return service.Get(id);
        
   }
    [HttpPost]
    public ActionResult Create(IceCreamModel newIceCream){
        var postedIceCream = service.Create(newIceCream);
        return CreatedAtAction(nameof(Create), new { id = postedIceCream.Id });
    }
    
    [HttpPut("{id}")]
    public ActionResult Update(int id,IceCreamModel newIceCream){
        var iceCream=service.Update(id,newIceCream);
        if(!iceCream)
        return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id){
        var iceCream=service.Delete(id);
        if(!iceCream)
           return NotFound();
        return NoContent();    
    }
}
