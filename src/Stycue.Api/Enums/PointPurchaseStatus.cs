namespace Stycue.Api.Enums
{
    public enum PointPurchaseStatus
    {
        // 已建立訂單，尚未付款
        Pending,
        // 付款成功，點數已入帳
        Paid,
        // 付款失敗
        Failed,
        // 使用者取消或訂單逾期取消
        Cancelled
    }
}
