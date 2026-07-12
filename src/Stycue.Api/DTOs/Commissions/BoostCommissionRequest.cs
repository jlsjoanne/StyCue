using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Stycue.Api.Options;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文加碼請求
    /// </summary>
    public class BoostCommissionRequest
    {
        /// <summary>
        /// 加碼積分
        /// </summary>
        [Range(1,int.MaxValue)]
        public int AdditionalPoints { get; set; }
    }
}
