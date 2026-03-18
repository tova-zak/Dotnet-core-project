namespace IceCream.Interfaces
{
    public interface IActiveUser
    {
        int? Id { get; set; }
        string? Role { get; set; }
        string? ShopName { get; set; }
    }
}
