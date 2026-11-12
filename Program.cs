using Microsoft.EntityFrameworkCore;
using RegistrationService.Data;
using RegistrationService.Middleware;
using RegistrationService.Repositories;
using RegistrationService.Services;

var builder = WebApplication.CreateBuilder(args);

// configuration (ortam degiskenleri: bkz. .env.example)
var dbPath = builder.Configuration["DB_PATH"] ?? "registrations.db";
var eventServiceUrl = builder.Configuration["EVENT_SERVICE_URL"] ?? "http://localhost:8002";
var port = builder.Configuration["PORT"] ?? "8004";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// database + repository + service (dependency injection)
builder.Services.AddDbContext<RegistrationDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<IRegistrationService, RegistrationAppService>();
builder.Services.AddHttpClient<IEventClient, HttpEventClient>(c =>
{
    c.BaseAddress = new Uri(eventServiceUrl);
    c.Timeout = TimeSpan.FromSeconds(5); // sonsuz bekleme yok
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<RegistrationDbContext>().Database.EnsureCreated();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
