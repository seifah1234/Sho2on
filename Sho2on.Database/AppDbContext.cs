using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sho2on.Database.Models;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Xml.Linq;

namespace Sho2on.Database
{
    public enum DocumentType
    {
        Company,
        Employee,
        Signed,
        Other
    }

    public class AppDbContext : DbContext
    {
        private static string _connectionString;
        //private string _connectionString = $"Server=192.168.100.3,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;";

        public static string CentralStoragePath
        {
            get
            {
                // محاولة الحصول من قاعدة البيانات أولاً
                using (var tempContext = new AppDbContext(_connectionString))
                {
                    var setting = tempContext.Settings
                        .FirstOrDefault();

                    if (setting != null && !string.IsNullOrEmpty(setting.CentralDocumentStoragePath))
                    {
                        // اختبار المسار
                        if (TestNetworkPath(setting.CentralDocumentStoragePath))
                        {
                            EnsureDirectoryExists(setting.CentralDocumentStoragePath);
                            return setting.CentralDocumentStoragePath;
                        }
                    }
                }

                // محاولة مسارات متعددة
                string[] networkPaths = {
                    @$"\\{App.ServerIP}\HR_Documents",  // IP ثابت
                    @"\\SERVER\HR_Documents",         // اسم السيرفر
                    GetLocalServerIPPath(),           // IP محلي تلقائي
                };

                foreach (var path in networkPaths)
                {
                    if (TestNetworkPath(path))
                    {
                        EnsureDirectoryExists(path);
                        return path;
                    }
                }

                // استخدام المسار المحلي
                return GetLocalPath();
            }
        }

        private static string GetLocalServerIPPath()
        {
            try
            {
                // الحصول على IP المحلي للسيرفر
                string localIP = GetLocalIPAddress();
                return $@"\\{localIP}\HR_Documents";
            }
            catch
            {
                return @"\\localhost\HR_Documents";
            }
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        private static string GetLocalPath()
        {
            string localPath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.CommonDocuments), "HR_Documents");

            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);

            return localPath;
        }

        private static bool TestNetworkPath(string path)
        {
            try
            {
                if (path.StartsWith(@"\\"))
                {
                    // اختبار Ping أولاً
                    string serverName = path.Substring(2).Split('\\')[0];

                    if (serverName.Contains("."))
                    {
                        // IP address
                        if (!PingHost(serverName))
                            return false;
                    }

                    // محاولة الوصول للمسار
                    return Directory.Exists(path) || Directory.CreateDirectory(path) != null;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool PingHost(string nameOrAddress)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(nameOrAddress, 2000); // 2 ثانية وقت انتظار
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                    // إنشاء المجلدات الفرعية
                    string[] subFolders = { "CompanyDocuments", "EmployeeDocuments", "SignedDocuments", "OtherDocuments" };
                    foreach (var folder in subFolders)
                    {
                        string folderPath = Path.Combine(path, folder);
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                    }
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }
        }

        // دالة محسنة للحصول على مسار الملف
        public static string GetDocumentPath(string fileName, DocumentType docType, bool createIfNotExists = true)
        {
            string storagePath = CentralStoragePath;
            string subFolder = docType switch
            {
                DocumentType.Company => "CompanyDocuments",
                DocumentType.Employee => "EmployeeDocuments",
                DocumentType.Signed => "SignedDocuments",
                _ => "OtherDocuments"
            };

            string fullPath = Path.Combine(storagePath, subFolder, fileName);

            if (createIfNotExists)
            {
                try
                {
                    string directory = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ واستخدام مسار بديل
                    LogError($"Failed to create directory: {ex.Message}");
                    return GetFallbackPath(fileName, docType);
                }
            }

            return fullPath;
        }

        private static string GetFallbackPath(string fileName, DocumentType docType)
        {
            // مسار بديل محلي
            string localAppPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
            string subFolder = docType switch
            {
                DocumentType.Company => "Company",
                DocumentType.Employee => "Employee",
                DocumentType.Signed => "Signed",
                _ => "Other"
            };

            string fallbackPath = Path.Combine(localAppPath, subFolder, fileName);

            string directory = Path.GetDirectoryName(fallbackPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return fallbackPath;
        }

        private static void LogError(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "document_errors.log");
                string logDir = Path.GetDirectoryName(logPath);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch
            {
                // تجاهل أخطاء التسجيل
            }
        }

        public AppDbContext() : base(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer($"Server=197.44.171.27,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;", sqlServerOptions =>
                    //.UseSqlServer($"Server=192.168.100.3,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;", sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    })
                    .Options)
        {
            //_connectionString = $"Server=192.168.100.3,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;";
            _connectionString = $"Server=197.44.171.27,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;";
        }

        public AppDbContext(string connectionString) : base(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(connectionString, sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    })
                    .Options)
            {
                _connectionString = connectionString;
            }

            // DbSets
            public DbSet<Branch> Branches { get; set; }
            public DbSet<Break> Breaks { get; set; }
            public DbSet<Degree> Degrees { get; set; }
            public DbSet<Department> Departments { get; set; }
            public DbSet<JobTitle> JobTitles { get; set; }
            public DbSet<Shift> Shifts { get; set; }
            public DbSet<User> Users { get; set; }
            public DbSet<Role> Roles { get; set; }
            public DbSet<Permission> Permissions { get; set; }
            public DbSet<RolePermission> RolePermissions { get; set; }
            public DbSet<UserRole> UserRoles { get; set; }
            public DbSet<UserBranch> UserBranches { get; set; }
            public DbSet<JobType> JobTypes { get; set; }
            public DbSet<HolidayType> HolidayTypes { get; set; }
            public DbSet<WeekHoliday> WeekHolidays { get; set; }
            public DbSet<LateOvertime> LateOvertimes { get; set; }
            public DbSet<Menu> Menus { get; set; }
            public DbSet<FingerPrint> FingerPrints { get; set; }
            public DbSet<Machine> Machines { get; set; }
            public DbSet<MachineData> MachineData { get; set; }
            public DbSet<Salary> Salaries { get; set; }
            public DbSet<Attendance> Attendances { get; set; }
            public DbSet<Procedure> Procedures { get; set; }
            public DbSet<Leave> Leaves { get; set; }
            public DbSet<LeaveBalance> LeaveBalances { get; set; }
            public DbSet<CompanyDocument> CompanyDocuments { get; set; }
            public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
            public DbSet<EmployeeEvaluation> EmployeeEvaluations { get; set; }
            public DbSet<EvaluationCriteria> EvaluationCriterias { get; set; }
            public DbSet<Setting> Settings { get; set; }
            public DbSet<LeaveType> LeaveTypes { get; set; }

        public DbSet<Loan> Loans { get; set; }

        public DbSet<LoanPayment> LoanPayments { get; set; }

        public DbSet<SalaryPayment> SalaryPayments { get; set; }

        public DbSet<FriendshipBox> FriendshipBoxes { get; set; }

        public DbSet<FriendshipBoxTransaction> FriendshipBoxTransactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
                {
                    optionsBuilder.UseSqlServer(_connectionString, sqlServerOptions =>
                    {
                        // إضافة EnableRetryOnFailure هنا
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);

                        sqlServerOptions.CommandTimeout(180); // 3 دقائق
                    });

                    // لتسهيل التشخيص (يمكن إزالتها في Production)
                    optionsBuilder.LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuting });
                    optionsBuilder.EnableSensitiveDataLogging();
                    optionsBuilder.EnableDetailedErrors();
                }
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<RolePermission>()
       .HasKey(rp => new { rp.RoleID, rp.PermissionID });

            modelBuilder.Entity<UserRole>()
       .HasKey(rp => new { rp.UserId, rp.RoleId});

            modelBuilder.Entity<UserBranch>()
       .HasKey(rp => new { rp.UserID, rp.BranchId});

            modelBuilder.Entity<RolePermission>()
       .HasOne(rp => rp.Role)
       .WithMany(r => r.RolePermissions)
       .HasForeignKey(rp => rp.RoleID);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionID);

            modelBuilder.Entity<User>()
                .HasOne(rp => rp.Manager)
                .WithMany(p => p.MyEmployees)
                .HasForeignKey(rp => rp.ManagerId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeEvaluation>()
                .HasOne(ur => ur.Employee)
                .WithMany(u => u.EmployeeEvaluations)
                .HasForeignKey(ur => ur.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeDocument>()
                .HasOne(ur => ur.Employee)
                .WithMany(u => u.EmployeeDocuments)
                .HasForeignKey(ur => ur.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.ApprovedByUser)
                .WithMany(u => u.ApprovedLoans)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Menu>()
                .HasOne(ur => ur.Parent)
                .WithMany(u => u.Children)
                .HasForeignKey(ur => ur.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FingerPrint>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.FingerPrints)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MachineData>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.MachineData)
                .HasForeignKey(ur => ur.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.Attendances)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<UserBranch>()
    .HasOne(ub => ub.User)
    .WithMany(u => u.UserBranches)
    .HasForeignKey(ub => ub.UserID);


            modelBuilder.Entity<UserBranch>()
                .HasOne(ub => ub.Branch)
                .WithMany(b => b.UserBranches)
                .HasForeignKey(ub => ub.BranchId);
        

        // =======================
        // Branch
        modelBuilder.Entity<Branch>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            // =======================
            // Break
            modelBuilder.Entity<Break>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.StartTime)
                      .HasColumnType("time(0)");

                entity.Property(e => e.EndTime)
                      .HasColumnType("time(0)");

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            // =======================
            // Degree
            modelBuilder.Entity<Degree>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            // =======================
            // Department
            modelBuilder.Entity<Department>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            // =======================
            // JobTitle
            modelBuilder.Entity<JobTitle>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            // =======================
            // WeekHolidays
            modelBuilder.Entity<WeekHoliday>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            // =======================
            // Shift
            modelBuilder.Entity<Shift>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.StartTime)
                      .HasColumnType("time(0)");

                entity.Property(e => e.EndTime)
                      .HasColumnType("time(0)");

                entity.Property(e => e.EditedAt)
                      .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<User>(entity =>
            {

                entity.HasKey(e => e.Id);

                entity.Property(e => e.FullName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NationalID).HasMaxLength(20);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                // Relations
                entity.HasOne(u => u.Branch).WithMany().HasForeignKey(u => u.BranchId);
                entity.HasOne(u => u.Department).WithMany().HasForeignKey(u => u.DepartmentId);
                entity.HasOne(u => u.JobTitle).WithMany().HasForeignKey(u => u.JobTitleId);
                entity.HasOne(u => u.Degree).WithMany().HasForeignKey(u => u.DegreeId);
                entity.HasOne(u => u.Shift).WithMany().HasForeignKey(u => u.ShiftId);
                entity.HasOne(u => u.Break).WithMany().HasForeignKey(u => u.BreakId);
                entity.HasOne(u => u.WeekHoliday).WithMany().HasForeignKey(u => u.WeekHolidayId);
            });

        }
    }
}
