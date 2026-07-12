using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stycue.Api.Data;
using Stycue.Api.Services.Interfaces;

namespace Stycue.Api.Controllers
{
    /// <summary>
    /// Web API專案相關檢查
    /// </summary>
    [Route("api/health")]
    [ApiController]
    public class HealthController : ControllerBase
    {

        private readonly AppDbContext _dbContext;
        private readonly IBlobStorageService _blobStorageService;

        public HealthController(AppDbContext appDbContext, IBlobStorageService blobStorageService)
        {
            _dbContext = appDbContext;
            _blobStorageService = blobStorageService;
        }


        /// <summary>
        /// 檢查 API 服務是否正常運作
        /// </summary>
        /// <returns>API 服務健康狀態</returns>
        /// <response code="200">API 服務正常運作</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                app = "StyCue.Api"
            });
        }

        /// <summary>
        /// 檢查資料庫連線是否正常
        /// </summary>
        /// <returns>資料庫連線狀態</returns>
        /// <response code="200">成功取得資料庫連線狀態</response>
        [HttpGet("db")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckDatabase()
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();

            return Ok(new
            {
                database = canConnect ? "connected" : "not connected"
            });
        }

        /// <summary>
        ///  檢查 Azure Blob Storage 連線是否正常
        /// </summary>
        /// <remarks>
        /// 用於確認後端是否可以連線到 Azure Blob Storage，並確認指定的 Blob container 是否存在。
        /// 此 API 主要供開發、部署與維運時檢查圖片儲存服務狀態。
        /// </remarks>
        /// <returns>Azure Blob Storage 連線健康狀態</returns>
        /// <response code="200">Azure Blob Storage 連線正常，且 container 可存取</response>
        /// <response code="503">Azure Blob Storage 無法連線，或指定的 container 不存在 / 無法存取</response>
        [HttpGet("blob")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CheckBlobStorage()
        {
            var canConnect = await _blobStorageService.CheckConnectionAsync();

            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    success = false,
                    service = "Azure Blob Storage",
                    status = "unhealthy",
                    message = "Can't connect to Azure Blob Storage or container doesn't exist."
                });
            }

            return Ok(new
            {
                success = true,
                service = "Azure Blob Storage",
                status = "healthy"
            });
        }
    }
}
