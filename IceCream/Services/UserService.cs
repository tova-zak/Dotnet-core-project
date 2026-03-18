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
        // דוגמאות התחלתיות של חנויות/משתמשים עם השדות החדשים
        this.list = new List<UserModel>
        {
            new UserModel{Id=1,ShopName="LeaShop",Email="lea@example.com",Address="Main St 1", Password=""},
           new UserModel{Id=2,ShopName="ToviShop",Email="tovi@example.com",Address="Market 5", Password=""},
           new UserModel{Id=3,ShopName="RutShop",Email="rut@example.com",Address="Center 10", Password=""}
        };
        // הסבר מקומי (עברית):
        // הנתונים של המשתמשים (החנויות) נשמרים לקובץ מקומי בתיקיית `Data` בשם `User.json`.
        // מכיוון שכל `UserModel` מכיל את השדה `IceCreams`, האוספים הפרטיים של כל חנות נשמרים יחד עם שאר פרטי המשתמש באותו קובץ.
        // השמירה/הטענה מתבצעת באמצעות `JsonSerializer` בפונקציות `saveToFile` ובבנאי.
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
        // ודא שסיסמת המשתמש נשמרת בצורה מאובטחת (hash) ולא כטקסט גלוי
        if (!string.IsNullOrEmpty(newUser.Password))
        {
            newUser.Password = PasswordHasher.Hash(newUser.Password);
        }
        else
        {
            // אם לא הוזנה סיסמה, אפשר להשאיר ריקה או לזרוק שגיאה — כאן נשמרת ריקה
            newUser.Password = string.Empty;
        }

        // ברירת מחדל לתפקיד
        if (string.IsNullOrWhiteSpace(newUser.Role))
            newUser.Role = "User";

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

        // אם נשלחה סיסמה חדשה, hash אותה לפני עדכון
        if (!string.IsNullOrEmpty(newUser.Password) && newUser.Password != user.Password)
        {
            // אם הערך שנשלח אינו נראה כמו hash (בדיקה פשוטה), נבצע hash
            if (!newUser.Password.Contains('.'))
                newUser.Password = PasswordHasher.Hash(newUser.Password);
        }
        else
        {
            // אם לא נשלחה סיסמה — נשמור את הסיסמה הקיימת
            newUser.Password = user.Password;
        }

        // אם לא נשלח שדה האוספים, נשמור את האוסף הקיים
        if (newUser.IceCreams == null)
            newUser.IceCreams = user.IceCreams;

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
