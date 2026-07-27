using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Web.Endpoints;
using Sho2on.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var connStr = builder.Configuration.GetConnectionString("Sho2onDB");
typeof(AppDbContext)
    .GetField("_connectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
    ?.SetValue(null, connStr);
// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(5)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<UiStateService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AttendanceService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Sho2on.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
