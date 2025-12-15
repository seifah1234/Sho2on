using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// قراءة Connection String من appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// إضافة DbContext مع الـ SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        sqlOptions.CommandTimeout(180);
    })
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// في Program.cs أو Startup.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFlutterApp");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
