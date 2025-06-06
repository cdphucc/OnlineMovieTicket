using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Services
{
    public interface IVietQRService
    {
        Task<VietQRResponseModel> GenerateQRCodeAsync(VietQRRequestModel request);
        Task<List<BankInfo>> GetBanksAsync();
        string GenerateLocalQRCode(string qrData);
    }
}