using System.ComponentModel.DataAnnotations;

namespace Stycue.Api.DTOs.Commissions
{
    /// <summary>
    /// 委託文加碼請求
    /// </summary>
    public class BoostCommissionRequest
    {
        /// <summary>
        /// 加碼積分，最低10積分
        /// </summary>
        [Range(10,int.MaxValue)]
        public int AdditionalPoints { get; set; }
    }
}
