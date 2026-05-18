using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using IceCream.Models;
using IceCream.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using IceCream.Hubs;
namespace IceCream.Controllers;


[ApiController]
[Route("[controller]")]
public class IceCreamController : ControllerBase
{  private readonly IOrderService service;
   private readonly IHubContext<NotificationHub> hubContext;

                      
    public IceCreamController(IOrderService IC, IHubContext<NotificationHub> hubContext)
    {
        this.service = IC;
        this.hubContext = hubContext;
    }
   
    [Authorize(Policy="AllUsers")]
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
        
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        var message = System.Text.Json.JsonSerializer.Serialize(new { 
            action = "ice_created", 
            id = newIceCream.Id, 
            name = newIceCream.Name,
            by = userId 
        });
        _ = NotificationHub.NotifyAdmins(hubContext, message);
        
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
        
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        var message = System.Text.Json.JsonSerializer.Serialize(new { 
            action = "ice_updated", 
            id = newIceCream.Id,
            name = newIceCream.Name,
            by = userId 
        });
        _ = NotificationHub.NotifyAdmins(hubContext, message);
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy="Admin")]
    public IActionResult Delete(int id){
        var iceCream=service.Get(id);
        if(iceCream==null)
           return NotFound();
        service.Delete(id);
        
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        var message = System.Text.Json.JsonSerializer.Serialize(new { 
            action = "ice_deleted", 
            id = id,
            by = userId 
        });
        _ = NotificationHub.NotifyAdmins(hubContext, message);
        
        return Content(service.Get().Count.ToString());    
    }
}
