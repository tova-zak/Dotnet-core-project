using System.Collections.Generic;
using IceCream.Models;

namespace IceCream.Interfaces
{
    public interface IOrderService
    {
       List<IceCreamModel> Get();
       IceCreamModel Get(int id);
       IceCreamModel Create(IceCreamModel newIceCream);
       bool Update(int id,IceCreamModel newIceCream);
       bool Delete(int id); 
    }
}

