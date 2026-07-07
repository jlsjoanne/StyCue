using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;

namespace Stycue.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbHealthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public DbHealthController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();

            return Ok(new
            {
                database = canConnect ? "connected" : "not connected"
            });
        }
    }
}
