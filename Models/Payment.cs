namespace OnlineMovieTicket.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; } // ID of the booking this payment is associated with
        public decimal Amount { get; set; } // Amount paid
        public string PaymentMethod { get; set; } // e.g., "Credit Card", "PayPal", "Cash"
        public string TransactionId { get; set; } // Transaction code from payment gateway
        public string Status { get; set; } // e.g., "Completed", "Pending", "Failed"
        public DateTime PaymentDate { get; set; } // Date and time of the payment
        //Navigation
        public Booking Booking { get; set; } // Navigation property to the booking associated with this payment

    }
}
