using Microsoft.EntityFrameworkCore;
using SaaS.Shared.Kernel.Extensions;
using SaaS.Notifications.Service.Consumers;
using SaaS.Notifications.Service.Infrastructure.Persistence;
using SaaS.Notifications.Service.Infrastructure.Email;
using SaaS.Notifications.Service.Features.Notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEventBus(builder.Configuration, [typeof(UserRegisteredConsumer).Assembly]);

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<INotificationSender, NotificationSender>();

builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.Run();

public partial class Program { }
