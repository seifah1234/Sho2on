using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public UsersController(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
            return Ok(await _db.Users.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
            var user = await _db.Users.FindAsync(id);
            return user == null ? NotFound() : Ok(user);
        }
    }
}
