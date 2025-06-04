namespace OnlineMovieTicket.Models
{
    public class Booking
    {
        public int Id { get; set; } // Unique identifier for the booking
        public string UserId { get; set; } // ID of the user who made the booking
        public DateTime BookingTime { get; set; } // Time when the booking was made
        public decimal TotalAmount { get; set; } // Total amount for the booking
        public string Status { get; set; } // e.g., "Confirmed", "Cancelled", "Pending"
        public required int PromotionId { get; set; } // ID of the promotion applied to the booking
        public required int PaymentId { get; set; } // ID of the payment method used for the booking
        public DateTime CreatedAt { get; set; } // Timestamp when the booking was created
        //Navigation
        public ApplicationUser User { get; set; } // Navigation property to the user who made the booking
        public Payment Payment { get; set; } // Navigation property to the payment details for this booking
        public ICollection<BookingDetail> BookingDetails { get; set; } // List of booking details for this booking
    }
}
