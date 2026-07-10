using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Stycue.Api.Services.Interfaces;
using Stycue.Api.Extensions;
using Stycue.Api.DTOs.Comm;
using Stycue.Api.DTOs.Images;

namespace Stycue.Api.Controllers
{
    
    [Authorize]
    [Route("api/images")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }

        /// <summary>
        /// 上傳委託文圖片
        /// </summary>
        /// <remarks>
        /// 使用 multipart/form-data 上傳圖片。圖片上傳後尚未綁定到委託文，
        /// 後續建立委託文時需將回傳的 imageId 放入 request body。
        /// 回傳的 url 為短效 Read-only SAS URL，可供前端預覽圖片。
        /// </remarks>
        /// <param name="request">圖片檔案與選填的服飾分類、品牌資訊</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>圖片 ID、用途、短效 SAS URL 與選填 metadata</returns>
        /// <response code="200">圖片上傳成功</response>
        /// <response code="400">圖片檔案為空、格式錯誤或超過大小限制</response>
        /// <response code="401">未登入或登入資訊無效</response>
        [HttpPost("commissions")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ImageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ImageResponse>),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ImageResponse>),StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCommissionImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _imageService.UploadCommissionImageAsync(userId, request, cancellationToken);

            if(!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// 上傳留言圖片
        /// </summary>
        /// <remarks>
        /// 使用 multipart/form-data 上傳圖片。圖片上傳後尚未綁定到留言，
        /// 後續建立留言時需將回傳的 imageId 放入 request body。
        /// 回傳的 url 為短效 Read-only SAS URL，可供前端預覽圖片。
        /// </remarks>
        /// <param name="request">圖片檔案與選填的服飾分類、品牌資訊</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>圖片 ID、用途、短效 SAS URL 與選填 metadata</returns>
        /// <response code="200">圖片上傳成功</response>
        /// <response code="400">圖片檔案為空、格式錯誤或超過大小限制</response>
        /// <response code="401">未登入或登入資訊無效</response>
        [HttpPost("comments")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ImageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<ImageResponse>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCommentImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            var result = await _imageService.UploadCommentImageAsync(userId, request, cancellationToken);

            if(!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// 刪除圖片
        /// </summary>
        /// <remarks>
        /// 僅圖片擁有者可以刪除圖片。此 API 會先 soft delete ImageAsset metadata，
        /// 再嘗試刪除 Azure Blob 實體檔案。
        /// </remarks>
        /// <param name="imageId">圖片資料 ID</param>
        /// <param name="cancellationToken">Request 取消通知</param>
        /// <returns>圖片刪除結果</returns>
        /// <response code="200">圖片刪除成功</response>
        /// <response code="400">圖片 ID 不合法，或圖片已刪除</response>
        /// <response code="401">未登入或登入資訊無效</response>
        /// <response code="403">沒有權限刪除此圖片</response>
        /// <response code="404">找不到指定圖片</response>
        [HttpDelete("{imageId:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteImage(int imageId, CancellationToken cancellationToken)
        {
            if(imageId <= 0)
            {
                return BadRequest(ApiResponse<object>.FailResult("圖片 ID 不合法", "INVALID_IMAGE_ID"));
            }

            var userId = User.GetUserId();

            var result = await _imageService.DeleteAsync(userId, imageId, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "IMAGE_NOT_FOUND" => NotFound(result),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, result),
                "IMAGE_ALREADY_DELETED" => BadRequest(result),
                _ => BadRequest(result)
            };
        }
    }
}
