using Stycue.Api.DTOs.Payment;

namespace Stycue.Api.Services.Interfaces
{
    public interface IEcpayPaymentGateway
    {
        EcpayCheckoutFormResponse CreateCheckoutForm(
            EcpayCheckoutRequest request);

        bool VerifyCheckMacValue(
            IReadOnlyDictionary<string, string> formFields);

        Task<EcpayQueryTradeInfoResponse> QueryTradeInfoAsync(
            string merchantTradeNo, CancellationToken cancellationToken = default);
    }
}
