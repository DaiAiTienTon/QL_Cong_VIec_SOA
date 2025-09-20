using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QL_Cong_Viec.Data;
using QL_Cong_Viec.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<FlightService>();
builder.Services.AddHttpClient<WikiService>();

builder.Services.AddHttpClient<AmadeusService>();
builder.Services.AddScoped<FlightAggregatorService>();

builder.Services.AddHttpClient<HotelService>();
builder.Services.AddHttpClient<CountryService>();
builder.Services.AddHttpClient<TimeService>();
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddHttpClient<CurrencyService>();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpClient<HotelService>();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
