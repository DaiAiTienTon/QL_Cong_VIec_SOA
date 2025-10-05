
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using QL_Cong_Viec.Data;
using QL_Cong_Viec.ESB.Implementation;
using QL_Cong_Viec.ESB.Interface;

using QL_Cong_Viec.ESB.Services;
using QL_Cong_Viec.Extensions; // For ESB extension methods
using QL_Cong_Viec.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

// ✅ Thêm Session và Memory Cache
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// Add HttpClients for existing services
builder.Services.AddHttpClient<FlightService>();
builder.Services.AddHttpClient<WikiService>();

builder.Services.AddHttpClient<HotelService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
builder.Services.AddHttpClient<CountryService>();
builder.Services.AddHttpClient<TimeService>();
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<CurrencyService>();

// Add existing services
builder.Services.AddScoped<FlightService>();
builder.Services.AddScoped<WikiService>();

builder.Services.AddScoped<HotelService>();
builder.Services.AddScoped<FlightAggregatorService>();

builder.Services.AddSingleton<CountryService>();
builder.Services.AddSingleton<TimeService>();
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<CurrencyService>();
// Add memory cache for ESB caching service
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

// Add ESB Architecture
builder.Services.AddESB(); // This extension method adds all ESB components

// Add Controllers with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity configuration
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    // Add additional identity options if needed
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

app.UseResponseCompression();
// Configure ESB after building the app
app.Services.ConfigureESB();

// Subscribe to ESB events for monitoring and logging
var serviceBus = app.Services.GetRequiredService<IServiceBus>();

// Subscribe to service events for logging
serviceBus.Subscribe<object>("ServiceRequestCompleted", async (data) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Service request completed: {Data}", data);
});

serviceBus.Subscribe<object>("ServiceRequestFailed", async (data) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Service request failed: {Data}", data);
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios
    app.UseHsts();
}
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Add this for Identity
app.UseAuthorization();

// Add health check endpoint for ESB services
app.MapGet("/health/services", async (HealthCheckService healthService) =>
{
    var health = await healthService.GetAllServiceHealthAsync();
    return Results.Json(health);
});

// Add ESB service registry endpoint
app.MapGet("/esb/services", (IServiceRegistry serviceRegistry) =>
{
    var services = serviceRegistry.GetAvailableServices();
    return Results.Json(new { Services = services, Count = services.Count });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Flights}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
