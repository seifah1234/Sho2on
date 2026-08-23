using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalaryController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SalaryController(AppDbContext db) => _db = db;

        [HttpGet("payslip/{userId}/{month}/{year}")]
        public async Task<IActionResult> GetPayslip(int userId, int month, int year)
        {
            var payment = await _db.SalaryPayments
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Month == month && s.Year == year);

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound("الموظف غير موجود");

            return Ok(new
            {
                EmployeeName = user.FullName,
                Month = month,
                Year = year,
                BasicSalary = payment?.BasicSalary ?? 0,
                TotalAdditions = payment?.TotalAdditions ?? 0,
                TotalDeductions = payment?.TotalDeductions ?? 0,
                NetSalary = payment?.NetSalary ?? 0,
                IsPaid = payment?.IsPaid ?? false,
                PaymentDate = payment?.PaymentDate
            });
        }
    }
}