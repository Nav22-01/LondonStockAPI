using Microsoft.EntityFrameworkCore;
using LondonStockAPI.Repository;
using LondonStockAPI.Repository.Interface;
using Microsoft.OpenApi.Models;
using LondonStockAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StockExchangeContext>(options =>
    options.UseInMemoryDatabase("StockDb"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => true);
    });
});

builder.Services.AddSignalR();
builder.Services.AddScoped<ITradeRepository, TradeRepository>();
builder.Services.AddControllers();
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "London Stock API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "London Stock API v1"));
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseWebSockets();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthorization(); 

app.MapControllers();
app.MapHub<TradeHub>("/tradehub");
app.Run();
