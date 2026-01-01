using IceCream.Services;
using MyMiddleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer; // ...existing code... add JWT bearer using
using Microsoft.IdentityModel.Tokens; // ...existing code... add identity model using

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOrderServices();
builder.Services.AddUserServices();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register authentication using JWT bearer tokens
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // register the JWT bearer authentication scheme
    .AddJwtBearer(options => // configure JWT bearer options
    {
        options.TokenValidationParameters = UserTokenService.GetTokenValidationParameters(); // use validation params from our token service
    });

// Register simple authorization policies used in controllers
builder.Services.AddAuthorization(options => // add authorization policies
{
    options.AddPolicy("AllUsers", policy => policy.RequireClaim("userFirstName")); // policy: any token that has 'userFirstName' claim
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

 app.UseDefaultFiles();
 app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication(); // ensure authentication middleware runs before authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
