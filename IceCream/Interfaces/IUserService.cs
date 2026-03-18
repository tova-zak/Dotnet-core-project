using System.Collections.Generic;
using IceCream.Models;

namespace IceCream.Interfaces
{
    public interface IUserService
    {
       List<UserModel> Get();
       UserModel Get(int id);
       UserModel Create(UserModel newUser);
       bool Update(int id,UserModel newUser);
       bool Delete(int id); 
    }
}
