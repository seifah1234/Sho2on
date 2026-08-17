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

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connStr,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

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

// جميع الخدمات
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<UiStateService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<LeaveTypeService>();
builder.Services.AddScoped<LeaveBalanceService>();
builder.Services.AddScoped<LeaveRequestService>();
builder.Services.AddScoped<LeaveManagementService>();
builder.Services.AddScoped<PermissionRequestService>();
builder.Services.AddScoped<PermissionManagementService>();
builder.Services.AddScoped<MissionService>();
builder.Services.AddScoped<AttendanceProcessingService>();
builder.Services.AddScoped<SalaryAttendanceCalculationService>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<BenefitService>();
builder.Services.AddScoped<SalaryCalculationService>();
builder.Services.AddScoped<SalaryService>();
builder.Services.AddScoped<SalarySettingService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<CompanyDocumentService>();
builder.Services.AddScoped<EmployeeDocumentService>();
builder.Services.AddScoped<EvaluationService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddSingleton<ChatTokenService>();
builder.Services.AddScoped<ChatConnectionService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<LateOvertimeService>();
builder.Services.AddScoped<SettingService>();
builder.Services.AddScoped<BreakService>();
builder.Services.AddScoped<BenefitTypeService>();
builder.Services.AddScoped<NotificationCenterService>();
builder.Services.AddHttpClient<InternalNotifyClient>();
builder.Services.AddScoped<BranchService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<AreaService>();
builder.Services.AddScoped<JobTitleService>();

builder.Services.AddScoped<QualificationService>();
builder.Services.AddScoped<ShiftService>();
builder.Services.AddScoped<OfficialHolidayService>();
builder.Services.AddScoped<WeekHolidayService>();
builder.Services.AddScoped<OfficialService>();
builder.Services.AddScoped<DepartmentTransferService>();

builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapDocumentEndpoints();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseDeveloperExceptionPage();

app.MapRazorComponents<Sho2on.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();