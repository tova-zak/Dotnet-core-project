using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using IceCream.Models;
using IceCream.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
namespace IceCream.Controllers;


[ApiController]
[Route("[controller]")]
public class IceCreamController : ControllerBase
{  private readonly IOrderService service;
   

                      
    public IceCreamController(IOrderService IC)
    {
        this.service = IC;
    }
   

    // דרישה: יש צורך בטוקן כדי לראות את רשימת הגלידות
    [Authorize(Policy="AllUsers")] // שורה זו דורשת שהקריאה תעשה עם טוקן שמקיים את מדיניות "AllUsers"
    [HttpGet()]
    public ActionResult<IEnumerable<IceCreamModel>> GetAll()=>
    service.Get();
    

    [HttpGet("{id}")]
    [Authorize(Policy="Admin")]
    public ActionResult<IceCreamModel> Get(int id)
    {
        var iceCream=service.Get(id);
        if(iceCream==null)
          return NotFound();
        return iceCream;
        
   }
    [HttpPost]
    [Authorize(Policy="Admin")]
    public IActionResult Create(IceCreamModel newIceCream){
        service.Create(newIceCream);
        return CreatedAtAction(nameof(Get),new{id=newIceCream.Id},newIceCream);
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy="Admin")]
    public IActionResult Update(int id,[FromBody]IceCreamModel newIceCream){
        if(id!=newIceCream.Id)
          return BadRequest();
        var existing=service.Get(id);
        if(existing==null)
          return NotFound();
        service.Update(id,newIceCream);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy="Admin")]
    public IActionResult Delete(int id){
        var iceCream=service.Get(id);
        if(iceCream==null)
           return NotFound();
        service.Delete(id);
        return Content(service.Get().Count.ToString());    
    }
}
