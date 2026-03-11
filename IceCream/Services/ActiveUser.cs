namespace IceCream.Services
{
    using IceCream.Interfaces;

    public class ActiveUser : IActiveUser
    {
        // פרטי המשתמש הפעיל ימולאו על ידי middleware או על ידי קוד שמגיע עם הבקשה
        public int? Id { get; set; }
        public string? Role { get; set; }
        public string? ShopName { get; set; }
    }
}