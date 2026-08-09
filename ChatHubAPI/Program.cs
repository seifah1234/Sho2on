using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sho2on.Database;
using System.Net;
using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("Sho2onDB");
typeof(AppDbContext)
    .GetField("_connectionString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
    ?.SetValue(null, connStr);

// ?? ??????? ??????? ??
var useAllInterfaces = builder.Configuration.GetValue<bool>("ServerSettings:UseAllInterfaces", true);
var apiPort = builder.Configuration.GetValue<int>("ServerSettings:ApiPort", 7001);

var urls = new List<string>
{
    $"http://0.0.0.0:{apiPort}",
    $"http://localhost:{apiPort}"
};

var localIPs = GetLocalIPAddresses();
foreach (var ip in localIPs.Where(ip => ip != "127.0.0.1" && ip != "0.0.0.0"))
{
    urls.Add($"http://{ip}:{apiPort}");
}

builder.WebHost.UseUrls(urls.Distinct().ToArray());

Console.WriteLine("SignalR Hub starting on:");
foreach (var url in urls.Distinct())
{
    Console.WriteLine($"   {url}/chatHub");
}

// ?? SignalR ??
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 512 * 1024; // 512 KB
});

// ?? CORS ??
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

// ?? Database ??

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connStr));

// ?? ????? ???? ??? ???? — ???? AppDbContext ??????? ???? Factory ??
builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// ?? JWT Authentication ??
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false,
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ?? UserId Provider ??
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHubAPI.ChatHub>("/chatHub").RequireAuthorization();

// ?? Endpoint ????????? ??????? — ???????? ???? ??????? ??
app.MapPost("/api/notify", async (NotifyRequest req, IHubContext<ChatHubAPI.ChatHub> hubContext, HttpContext http, IConfiguration config) =>
{
    var apiKey = http.Request.Headers["X-Internal-Key"].ToString();
    if (apiKey != config["InternalApiKey"])
        return Results.Unauthorized();

    await hubContext.Clients.User(req.UserId.ToString())
        .SendAsync("ReceiveNotification", req.Title, req.Message, req.Icon, req.Url);

    return Results.Ok();
});

app.MapGet("/", () => Results.Ok(new
{
    Status = "Running",
    ServerTime = DateTime.Now,
    LocalIPs = localIPs,
    HubUrl = "/chatHub"
}));

app.Run();

static List<string> GetLocalIPAddresses()
{
    var ips = new List<string>();
    try
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                ips.Add(ip.ToString());
            }
        }
    }
    catch { }

    if (!ips.Contains("127.0.0.1")) ips.Add("127.0.0.1");
    return ips;
}

record NotifyRequest(int UserId, string Title, string Message, string Icon, string? Url);

public class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
    }
}



