using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Models.ViewModels
{
    public class TransactionHistoryViewModel
    {
        public List<Booking> Bookings { get; set; } = new List<Booking>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string StatusFilter { get; set; } = "";

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}