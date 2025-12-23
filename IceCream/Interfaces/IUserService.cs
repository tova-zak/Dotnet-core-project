using System.Threading.Tasks;
using IceCream.Models;

namespace IceCream.Intrfaces
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
