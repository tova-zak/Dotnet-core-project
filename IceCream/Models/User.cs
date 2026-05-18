namespace IceCream.Models;

public class UserModel
{
    public int Id { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; 
    public List<IceCreamModel> IceCreams { get; set; } = new List<IceCreamModel>();

}
