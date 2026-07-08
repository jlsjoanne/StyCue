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

        /// <summary>
        /// 檢查資料庫連線是否正常
        /// </summary>
        /// <returns>資料庫連線狀態</returns>
        /// <response code="200">成功取得資料庫連線狀態</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
