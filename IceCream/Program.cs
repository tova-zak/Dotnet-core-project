using IceCream.Services;
using MyMiddleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer; // ...existing code... add JWT bearer using
using Microsoft.IdentityModel.Tokens; // ...existing code... add identity model using

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAppServices();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register authentication using JWT bearer tokens
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // register the JWT bearer authentication scheme
    .AddJwtBearer(options => // configure JWT bearer options
    {
        options.TokenValidationParameters = UserTokenService.GetTokenValidationParameters(); // use validation params from our token service
        // enable SignalR to receive access token from query string if needed
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Register simple authorization policies used in controllers
builder.Services.AddAuthorization(options => // add authorization policies
{
    // שינינו את שם ה-claim שנדרש למדיניות - כעת נשתמש ב-userShopName
    options.AddPolicy("AllUsers", policy => policy.RequireClaim("userShopName")); // policy: any token that has 'userShopName' claim
    options.AddPolicy("Admin", policy => policy.RequireClaim("type", "Admin")); // policy: only tokens with claim type=Admin
});

var app = builder.Build();
// app.UseMyLogMiddleware();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var defaultFilesOptions = new Microsoft.AspNetCore.Builder.DefaultFilesOptions();
// שיניתי כאן את סדר עמודי ברירת המחדל כך שיעמוד "index.html" יוצג תמיד כשהמשתמש נכנס לשורש האתר.
// עמוד ההתחברות נשאר זמין בכתובת /html/login.html וניתן להגיע אליו בלחיצה על הכפתור "התחברות" בעמוד index.
defaultFilesOptions.DefaultFileNames.Clear();
// הצג את index.html ראשון (כדי שעמוד הנחיתה תמיד יוצג בהתחלה)
defaultFilesOptions.DefaultFileNames.Add("index.html");
// שמור את דף ההתחברות ברשימה כך שניתן להגיע אליו כ/default file אם רצינו, אבל הוא עכשיו שני
defaultFilesOptions.DefaultFileNames.Add("html/login.html");
// הנחיה: להזיז את ההפניה ל-HTTPS לפני שרות הקבצים הסטטיים כדי שכל הבקשות יופנו ל-HTTPS
app.UseHttpsRedirection();
app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();
app.UseAuthentication(); // ensure authentication middleware runs before authorization
app.UseActiveUser();
app.UseMyLogMiddleware();
app.UseAuthorization();

app.MapControllers();

// SignalR hub mapping
app.MapHub<IceCream.Hubs.NotificationHub>("/hubs/notify");

app.Run();
