using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sho2on.API.Models;
using System;

public class AppDbContext : DbContext
    {
        private string _connectionString;
        //private string _connectionString = $"Server=192.168.100.3,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;";


    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


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

            modelBuilder.Entity<RolePermission>()
       .HasKey(rp => new { rp.RoleID, rp.PermissionID });

            modelBuilder.Entity<UserRole>()
       .HasKey(rp => new { rp.UserId, rp.RoleId });

            modelBuilder.Entity<UserBranch>()
       .HasKey(rp => new { rp.UserID, rp.BranchId });

            modelBuilder.Entity<RolePermission>()
       .HasOne(rp => rp.Role)
       .WithMany(r => r.RolePermissions)
       .HasForeignKey(rp => rp.RoleID);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionID);

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
                .HasForeignKey(ub => ub.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBranch>()
                .HasOne(ub => ub.Branch)
                .WithMany()
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

