using Microsoft.AspNetCore.Mvc;
using IceCream.Models;
using IceCream.Interfaces;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Linq;

namespace IceCream.Services{

public class OrderService:IOrderService
{
    private /*static */List<IceCreamModel> list;
    private string filePath;
        public OrderService()
        {
            this.list = new List<IceCreamModel>()
            {
           new IceCreamModel{Id=1,Name="American",IsDiary=true},
           new IceCreamModel{Id=2,Name="Vanil",IsDiary=true},
           new IceCreamModel{Id=3,Name="ShokoShoko",IsDiary=false}
            };
            this.filePath = Path.Combine("Data", "IceCream.json");
            if (File.Exists(filePath))
            {
                using (var jsonFile = File.OpenText(filePath))
                {
                    var content = jsonFile.ReadToEnd();
                    list = JsonSerializer.Deserialize<List<IceCreamModel>>(content,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? list;
                }
            }
        }
         private void saveToFile()
        {
            var text = JsonSerializer.Serialize(list);
            File.WriteAllText(filePath, text);
        }

    public List<IceCreamModel> Get()
    {
        return list;
    }

    private IceCreamModel find(int id)
    {
        return list.FirstOrDefault(p => p.Id == id);

    }

    public IceCreamModel Get(int id) => find(id);

    public IceCreamModel Create(IceCreamModel newIceCream)
    {
        var maxId = list.Any() ? list.Max(p => p.Id) : 0;
        newIceCream.Id = maxId + 1;
        list.Add(newIceCream);
        saveToFile();
        return newIceCream;
        
    }

    public bool Update(int id, IceCreamModel newIceCream)
    {
        var iceCream = find(id);
        if (iceCream == null)
            return false;
        if (iceCream.Id != newIceCream.Id)
            return false;

        var index = list.IndexOf(iceCream);
        list[index] = newIceCream;
        saveToFile();

        return true;
    }

    public bool Delete(int id)
    {
        var iceCream = find(id);
        if (iceCream == null)
            return false;
        list.Remove(iceCream);
        saveToFile();
        return true;
        
    }
    }
    public static class OrderServiceExtension
    {
        public static void AddOrderServices(this IServiceCollection services)
        {
            services.AddSingleton<IOrderService, OrderService>();
            // services.AddSingleton<IOrderSender, OrderSenderHttp>();
            //services.AddScope<IOrderManager, OrderManager>();
            //services.AddTransient<IOrderSender, OrderSenderHttp>();            
        }
    }
}
