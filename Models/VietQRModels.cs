namespace OnlineMovieTicket.Models
{
    public class VietQRRequestModel
    {
        public int BookingId { get; set; }
        public string AccountNo { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string BankId { get; set; } = "";
        public decimal Amount { get; set; }
        public string Description { get; set; } = "";
        public string Template { get; set; } = "compact2";
    }

    public class VietQRResponseModel
    {
        public bool Success { get; set; }
        public string QRCode { get; set; } = ""; // Base64 image
        public string QRDataURL { get; set; } = ""; // Quick response data
        public string BookingId { get; set; } = "";
        public decimal Amount { get; set; }
        public string Description { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public class BankInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Bin { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Logo { get; set; } = "";
    }
}