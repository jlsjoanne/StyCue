using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 選擇最佳留言請求
    /// </summary>
    public class SelectBestCommentRequest
    {
        /// <summary>
        /// 留言Id
        /// 委託者選擇哪一則留言作為最佳留言
        /// </summary>
        [Range(1,int.MaxValue)]
        public int CommentId { get; set; }
    }
}
