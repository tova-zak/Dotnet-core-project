using IceCream.Services;
using MyMiddleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOrderServices();
builder.Services.AddUserServices();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
app.UseAuthentication();//
app.UseAuthorization();

app.MapControllers();

app.Run();
