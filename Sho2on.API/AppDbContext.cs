using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sho2on.Database.Models;
using System;

public class AppDbContext : DbContext
    {
        private string _connectionString;
        //private string _connectionString = $"Server=192.168.100.3,1433;Database=Original;User Id=OR;Password=OriginalIBS2025;" + "Pooling=true;" + "Max Pool Size=100;" + "Min Pool Size=5;" + "Connection Lifetime=300;" + "Connection Timeout=30;" + "TrustServerCertificate=True;";


    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


    // DbSets
    public DbSet<Area> Areas { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatAttachment> ChatAttachments { get; set; }

    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<UserTask> UserTasks { get; set; }
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

    public DbSet<ChatUserStatus> ChatUserStatuses { get; set; }
    public DbSet<FriendshipBoxTransaction> FriendshipBoxTransactions { get; set; }
    public DbSet<EmployeePermission> EmployeePermissions { get; set; }
    public DbSet<Qualification> Qualifications { get; set; }

    // أضف الـ DbSets دول
    public DbSet<ChatGroup> ChatGroups { get; set; }
    public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }
    public DbSet<ChatGroupMessage> ChatGroupMessages { get; set; }
    public DbSet<ChatGroupAttachment> ChatGroupAttachments { get; set; }
    public DbSet<ChatGroupMessageRead> ChatGroupMessageReads { get; set; }
    public DbSet<Offical> Officals { get; set; }

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
   .HasKey(rp => new { rp.UserId, rp.RoleId });

        modelBuilder.Entity<UserBranch>()
   .HasKey(rp => new { rp.UserID, rp.BranchId });

        modelBuilder.Entity<RolePermission>()
   .HasOne(rp => rp.Role)
   .WithMany(r => r.RolePermissions)
   .HasForeignKey(rp => rp.RoleID);

        modelBuilder.Entity<ChatUserStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Chat)
                  .WithMany()
                  .HasForeignKey(e => e.ChatId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionID);

        modelBuilder.Entity<User>()
            .HasOne(rp => rp.Manager)
            .WithMany(p => p.MyEmployees)
            .HasForeignKey(rp => rp.ManagerId);

        modelBuilder.Entity<UserTask>()
            .HasOne(rp => rp.AssignedToUser)
            .WithMany(p => p.AssignedToTasks)
            .HasForeignKey(rp => rp.AssignedToUserId);

        modelBuilder.Entity<Chat>()
            .HasOne(rp => rp.FirstUser)
            .WithMany(p => p.SenderChats)
            .HasForeignKey(rp => rp.FirstUserId);

        modelBuilder.Entity<Chat>()
            .HasOne(rp => rp.SecondUser)
            .WithMany(p => p.ReceiverChats)
            .HasForeignKey(rp => rp.SecondUserId);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(rp => rp.Chat)
            .WithMany(p => p.Messages)
            .HasForeignKey(rp => rp.ChatId);

        modelBuilder.Entity<UserTask>()
            .HasOne(rp => rp.AssignedByUser)
            .WithMany(p => p.AssignedByTasks)
            .HasForeignKey(rp => rp.AssignedByUserId);


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

        modelBuilder.Entity<EmployeePermission>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.EmployeePermissions)
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

        modelBuilder.Entity<Branch>()
            .HasOne(b => b.Area)
            .WithMany(a => a.Branches)
            .HasForeignKey(b => b.AreaId);

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
            entity.HasOne(u => u.Branch).WithMany(b => b.Users).HasForeignKey(u => u.BranchId);
            entity.HasOne(u => u.Department).WithMany(d => d.Users).HasForeignKey(u => u.DepartmentId);
            entity.HasOne(u => u.JobTitle).WithMany(j => j.Users).HasForeignKey(u => u.JobTitleId);
            entity.HasOne(u => u.Degree).WithMany(j => j.Users).HasForeignKey(u => u.DegreeId);
            entity.HasOne(u => u.Shift).WithMany().HasForeignKey(u => u.ShiftId);
            entity.HasOne(u => u.Break).WithMany().HasForeignKey(u => u.BreakId);
            entity.HasOne(u => u.WeekHoliday).WithMany().HasForeignKey(u => u.WeekHolidayId);
        });

    }
}

