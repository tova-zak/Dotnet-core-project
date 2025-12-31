using Microsoft.AspNetCore.Mvc;
using IceCream.Models;
using IceCream.Interfaces;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace IceCream.Services{

public class UserService:IUserService
{
    private /*static */List<UserModel> list;
     private string filePath;
    public UserService()
    {
        this.list = new List<UserModel>
        {
            new UserModel{Id=1,FirstName="Lea",LastName="Rainer"},
           new UserModel{Id=2,FirstName="Tovi",LastName="Zak"},
           new UserModel{Id=3,FirstName="Rut",LastName="levi"}
        };
        this.filePath = Path.Combine("Data", "User.json");
        if (File.Exists(filePath))
        {
            using (var jsonFile = File.OpenText(filePath))
            {
                var content = jsonFile.ReadToEnd();
                list = JsonSerializer.Deserialize<List<UserModel>>(content,
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

    public List<UserModel> Get()
    {
        return list;
    }

    public int Count => list?.Count ?? 0;

    private UserModel find(int id)
    {
        return list.FirstOrDefault(p => p.Id == id);

    }

    public UserModel Get(int id) => find(id);

    public UserModel Create(UserModel newUser)
    {
        var maxId = list.Any() ? list.Max(p => p.Id) : 0;
        newUser.Id = maxId + 1;
        list.Add(newUser);
        saveToFile();
        return newUser;
    }

    public bool Update(int id, UserModel newUser)
    {
        var user = find(id);
        if (user == null)
            return false;
        if (user.Id != newUser.Id)
            return false;

        var index = list.IndexOf(user);
        list[index] = newUser;
saveToFile();
        return true;
    }

    public bool Delete(int id)
    {
        var user = find(id);
        if (user == null)
            return false;
        list.Remove(user);
        saveToFile();
        return true;
    }
    }
    // public static class UserServiceExtension
    // {
    //     public static void AddUserServices(this IServiceCollection services)
    //     {
    //         services.AddSingleton<IUserService, UserService>();
    //         // services.AddSingleton<IUserSender, UserSenderHttp>();
    //         //services.AddScope<IUserManager, UserManager>();
    //         //services.AddTransient<IUserSender, UserSenderHttp>();            
    //     }
    // }
    public static class UserServiceExtension
    {
        public static void AddUserServices(this IServiceCollection services)
        {
            services.AddSingleton<IUserService, UserService>();
            // services.AddSingleton<IOrderSender, OrderSenderHttp>();
            //services.AddScope<IOrderManager, OrderManager>();
            //services.AddTransient<IOrderSender, OrderSenderHttp>();            
        }
    }
}
