// FriendshipBoxService.cs
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Threading.Tasks;

namespace HR_Application.Services
{
    public class FriendshipBoxService
    {
        private readonly AppDbContext _context;
        private const int DEFAULT_BOX_ID = 1; // ID صندوق الزمالة الافتراضي

        public FriendshipBoxService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetEmployeeFriendshipBoxAmountAsync(int userId)
        {
            var salary = await _context.Salaries
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Type == 13); // Type 13 = صندوق الزمالة

            return salary?.Amount ?? 0;
        }

        public async Task<List<User>> GetManagersAsync()
        {
            return await _context.Users
                .Include(u => u.JobTitle)
                .Where(u => u.JobTitle.IsManager.HasValue && u.JobTitle.IsManager.Value)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        // الحصول على صندوق الزمالة (يتم إنشاؤه إذا لم يكن موجوداً)
        public async Task<FriendshipBox> GetOrCreateFriendshipBoxAsync()
        {
            var box = await _context.FriendshipBoxes.FindAsync(DEFAULT_BOX_ID);

            if (box == null)
            {
                box = new FriendshipBox
                {
                    Id = DEFAULT_BOX_ID,
                    Name = "صندوق الزمالة المشترك",
                    CurrentBalance = 0,
                    TotalDeposits = 0,
                    TotalLoans = 0,
                    TotalRepayments = 0,
                    DeductionPercentage = 2.0m,
                    Description = "الصندوق المشترك لجميع الموظفين",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _context.FriendshipBoxes.AddAsync(box);
                await _context.SaveChangesAsync();
            }

            return box;
        }

        // تسجيل إيداع (خصم من الراتب)
        public async Task<FriendshipBoxTransaction> RecordDepositAsync(int userId, decimal amount, int salaryPaymentId, string description = "خصم من الراتب")
        {
            var box = await GetOrCreateFriendshipBoxAsync();

            var transaction = new FriendshipBoxTransaction
            {
                FriendshipBoxId = box.Id,
                UserId = userId,
                TransactionType = "Deposit",
                Amount = amount,
                BalanceBefore = box.CurrentBalance,
                BalanceAfter = box.CurrentBalance + amount,
                Description = description,
                SalaryPaymentId = salaryPaymentId,
                TransactionDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            // تحديث رصيد الصندوق
            box.CurrentBalance += amount;
            box.TotalDeposits += amount;
            box.UpdatedAt = DateTime.Now;

            await _context.FriendshipBoxTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        // تسجيل سحب (سلفة)
        public async Task<FriendshipBoxTransaction> RecordWithdrawalAsync(int userId, decimal amount, int loanId, string reason = "سلفة")
        {
            var box = await GetOrCreateFriendshipBoxAsync();

            // التحقق من أن الرصيد كافي
            if (box.CurrentBalance < amount)
            {
                throw new InvalidOperationException($"رصيد صندوق الزمالة غير كافي. الرصيد المتاح: {box.CurrentBalance:N2}");
            }

            var transaction = new FriendshipBoxTransaction
            {
                FriendshipBoxId = box.Id,
                UserId = userId,
                TransactionType = "Withdrawal",
                Amount = -amount, // سالب لأنه سحب
                BalanceBefore = box.CurrentBalance,
                BalanceAfter = box.CurrentBalance - amount,
                Description = reason,
                LoanId = loanId,
                TransactionDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            // تحديث رصيد الصندوق
            box.CurrentBalance -= amount;
            box.TotalLoans += amount;
            box.UpdatedAt = DateTime.Now;

            await _context.FriendshipBoxTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        // تسجيل سداد سلفة
        public async Task<FriendshipBoxTransaction> RecordRepaymentAsync(int userId, decimal amount, int loanPaymentId, string description = "سداد سلفة")
        {
            var box = await GetOrCreateFriendshipBoxAsync();

            var transaction = new FriendshipBoxTransaction
            {
                FriendshipBoxId = box.Id,
                UserId = userId,
                TransactionType = "Repayment",
                Amount = amount,
                BalanceBefore = box.CurrentBalance,
                BalanceAfter = box.CurrentBalance + amount,
                Description = description,
                TransactionDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            // تحديث رصيد الصندوق
            box.CurrentBalance += amount;
            box.TotalRepayments += amount;
            box.UpdatedAt = DateTime.Now;

            await _context.FriendshipBoxTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        // الحصول على رصيد الصندوق
        public async Task<decimal> GetCurrentBalanceAsync()
        {
            var box = await GetOrCreateFriendshipBoxAsync();
            return box.CurrentBalance;
        }

        // التحقق من إمكانية السحب
        public async Task<bool> CanWithdrawAsync(decimal amount)
        {
            var balance = await GetCurrentBalanceAsync();
            return balance >= amount;
        }

        // الحصول على نسبة الخصم الحالية
        public async Task<decimal> GetDeductionPercentageAsync()
        {
            var box = await GetOrCreateFriendshipBoxAsync();
            return box.DeductionPercentage;
        }

        // تحديث نسبة الخصم
        public async Task UpdateDeductionPercentageAsync(decimal percentage)
        {
            if (percentage < 0 || percentage > 100)
                throw new ArgumentException("النسبة يجب أن تكون بين 0 و 100");

            var box = await GetOrCreateFriendshipBoxAsync();
            box.DeductionPercentage = percentage;
            box.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        // الحصول على حركات الصندوق
        public async Task<List<FriendshipBoxTransaction>> GetTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.FriendshipBoxTransactions
                .Include(t => t.User)
                .Include(t => t.Loan)
                .Where(t => t.FriendshipBoxId == DEFAULT_BOX_ID);

            if (fromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.TransactionDate <= toDate.Value);

            return await query.OrderByDescending(t => t.TransactionDate).ToListAsync();
        }

        // الحصول على إحصائيات الصندوق
        public async Task<FriendshipBoxStatistics> GetStatisticsAsync()
        {
            var box = await GetOrCreateFriendshipBoxAsync();

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var transactions = await _context.FriendshipBoxTransactions
                .Where(t => t.FriendshipBoxId == DEFAULT_BOX_ID)
                .ToListAsync();

            return new FriendshipBoxStatistics
            {
                CurrentBalance = box.CurrentBalance,
                TotalDeposits = box.TotalDeposits,
                TotalLoans = box.TotalLoans,
                TotalRepayments = box.TotalRepayments,
                MonthlyDeposits = transactions
                    .Where(t => t.TransactionType == "Deposit" && t.TransactionDate >= monthStart)
                    .Sum(t => t.Amount),
                MonthlyLoans = transactions
                    .Where(t => t.TransactionType == "Withdrawal" && t.TransactionDate >= monthStart)
                    .Sum(t => Math.Abs(t.Amount)),
                YearlyDeposits = transactions
                    .Where(t => t.TransactionType == "Deposit" && t.TransactionDate >= yearStart)
                    .Sum(t => t.Amount),
                YearlyLoans = transactions
                    .Where(t => t.TransactionType == "Withdrawal" && t.TransactionDate >= yearStart)
                    .Sum(t => Math.Abs(t.Amount))
            };
        }
    }

    public class FriendshipBoxStatistics
    {
        public decimal CurrentBalance { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalLoans { get; set; }
        public decimal TotalRepayments { get; set; }
        public decimal MonthlyDeposits { get; set; }
        public decimal MonthlyLoans { get; set; }
        public decimal YearlyDeposits { get; set; }
        public decimal YearlyLoans { get; set; }
    }
}