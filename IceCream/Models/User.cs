namespace IceCream.Models;

public class UserModel
{
    public int Id { get; set; }

    // שינויים: מודל המשתמש מותאם למצב שבו כל משתמש מייצג חנות.
    // במקום שדות של שם פרטי ושם משפחה אנו שומרים פרטים של החנות:
    // - Password: סיסמא של החנות
    // - ShopName: שם החנות (ישמש גם כ"שם משתמש" לכניסה)
    // - Email: כתובת דוא"ל של החנות
    // - Address: כתובת החנות
    // - Role: האם המשתמש הוא "Admin" או "User"
    // הסיבה: בפרויקט זה כל משתמש הוא בעצם חנות שמנהלת אוסף גלידות פרטי.
    public string Password { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // ברירת מחדל: User

    // הסבר כללי: לכל משתמש (חנות) יש שדה `IceCreams` שהוא האוסף הפרטי של הגלידות שלה.
    // בקרות הגישה מיושמות ב־API כך שמשתמש רגיל יכול לראות/לשנות רק את האוסף שלו,
    // והמנהל יכול לראות/לשנות את כל האוספים.
    public List<IceCreamModel> IceCreams { get; set; } = new List<IceCreamModel>();

}
