namespace IceCream.Services
{
    using IceCream.Interfaces;

    public class ActiveUser : IActiveUser
    {
        public int? Id { get; set; }
        public string? Role { get; set; }
        public string? ShopName { get; set; }
    }
}