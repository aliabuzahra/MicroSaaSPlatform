using Microsoft.EntityFrameworkCore;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Notifications.Service.Consumers;
using SaaS.Notifications.Service.Features;
using SaaS.Notifications.Service.Infrastructure.Email;
using SaaS.Notifications.Service.Infrastructure.Persistence;
using SaaS.Notifications.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEventBus(builder.Configuration, [typeof(UserRegisteredConsumer).Assembly]);

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.MapNotificationEndpoints();
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Notifications" }));

app.Run();

public partial class Program { }
