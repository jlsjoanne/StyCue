using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Stycue.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {

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
    }
}
