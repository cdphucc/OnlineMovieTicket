using OnlineMovieTicket.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;

namespace OnlineMovieTicket.Services
{
    public class VietQRService : IVietQRService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly string _baseUrl = "https://img.vietqr.io/image";

        public VietQRService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<VietQRResponseModel> GenerateQRCodeAsync(VietQRRequestModel request)
        {
            try
            {
                // Tạo URL VietQR
                var qrUrl = $"{_baseUrl}/{request.BankId}-{request.AccountNo}-{request.Template}.png" +
                           $"?amount={request.Amount}" +
                           $"&addInfo={Uri.EscapeDataString(request.Description)}" +
                           $"&accountName={Uri.EscapeDataString(request.AccountName)}";

                // Download QR image
                var imageBytes = await _httpClient.GetByteArrayAsync(qrUrl);
                var base64Image = Convert.ToBase64String(imageBytes);

                // Tạo QR Data cho thanh toán
                var qrData = GenerateQRData(request);

                return new VietQRResponseModel
                {
                    Success = true,
                    QRCode = $"data:image/png;base64,{base64Image}",
                    QRDataURL = qrData,
                    BookingId = request.BookingId.ToString(),
                    Amount = request.Amount,
                    Description = request.Description
                };
            }
            catch (Exception ex)
            {
                return new VietQRResponseModel
                {
                    Success = false,
                    ErrorMessage = $"Lỗi tạo QR Code: {ex.Message}"
                };
            }
        }

        public async Task<List<BankInfo>> GetBanksAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.vietqr.io/v2/banks");
                var bankResponse = JsonSerializer.Deserialize<BankListResponse>(response);

                return bankResponse?.data?.Select(bank => new BankInfo
                {
                    Id = bank.id.ToString(),
                    Name = bank.name,
                    Code = bank.code,
                    Bin = bank.bin,
                    ShortName = bank.shortName,
                    Logo = bank.logo
                }).ToList() ?? new List<BankInfo>();
            }
            catch
            {
                // Trả về danh sách ngân hàng phổ biến nếu API lỗi
                return GetDefaultBanks();
            }
        }

        public string GenerateLocalQRCode(string qrData)
        {
            try
            {
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
            }
            catch
            {
                return "";
            }
        }

        private string GenerateQRData(VietQRRequestModel request)
        {
            // Format theo chuẩn VietQR
            return $"2|99|{request.BankId}|{request.AccountNo}|{request.AccountName}|{request.Amount}|{request.Description}|VND";
        }

        private List<BankInfo> GetDefaultBanks()
        {
            return new List<BankInfo>
            {
                new BankInfo { Id = "970415", Name = "Ngân hàng TMCP Công thương Việt Nam", Code = "ICB", ShortName = "VietinBank" },
                new BankInfo { Id = "970436", Name = "Ngân hàng TMCP Ngoại thương Việt Nam", Code = "VCB", ShortName = "Vietcombank" },
                new BankInfo { Id = "970418", Name = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam", Code = "BIDV", ShortName = "BIDV" },
                new BankInfo { Id = "970403", Name = "Ngân hàng TMCP Sài Gòn Thương Tín", Code = "STB", ShortName = "Sacombank" },
                new BankInfo { Id = "970422", Name = "Ngân hàng TMCP Quân đội", Code = "MB", ShortName = "MBBank" },
                new BankInfo { Id = "970407", Name = "Ngân hàng TMCP Kỹ thương Việt Nam", Code = "TCB", ShortName = "Techcombank" },
                new BankInfo { Id = "970432", Name = "Ngân hàng TMCP Việt Nam Thịnh Vượng", Code = "VPB", ShortName = "VPBank" },
                new BankInfo { Id = "970423", Name = "Ngân hàng TMCP Tiên Phong", Code = "TPB", ShortName = "TPBank" }
            };
        }

        // Helper classes for API response
        private class BankListResponse
        {
            public string code { get; set; } = "";
            public string desc { get; set; } = "";
            public List<BankData> data { get; set; } = new();
        }

        private class BankData
        {
            public int id { get; set; }
            public string name { get; set; } = "";
            public string code { get; set; } = "";
            public string bin { get; set; } = "";
            public string shortName { get; set; } = "";
            public string logo { get; set; } = "";
        }
    }
}