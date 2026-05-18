using IceCream.Services;
using MyMiddleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAppServices();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) 
    .AddJwtBearer(options => 
    {
        options.TokenValidationParameters = UserTokenService.GetTokenValidationParameters(); 
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

 
builder.Services.AddAuthorization(options => /
{
    options.AddPolicy("AllUsers", policy => policy.RequireClaim("userShopName"));
    options.AddPolicy("Admin", policy => policy.RequireClaim("type", "Admin")); 
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var defaultFilesOptions = new Microsoft.AspNetCore.Builder.DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("index.html");
defaultFilesOptions.DefaultFileNames.Add("html/login.html");
app.UseHttpsRedirection();
app.UseDefaultFiles(defaultFilesOptions);
app.UseStaticFiles();
app.UseAuthentication(); 
app.UseMyLogMiddleware();
app.UseAuthorization();
app.MapControllers();
app.MapHub<IceCream.Hubs.NotificationHub>("/hubs/notify");
app.Run();
