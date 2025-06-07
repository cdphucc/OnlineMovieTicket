using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMovieTicket.Data;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Models.ViewModels;
using System.Security.Claims;

namespace OnlineMovieTicket.Controllers
{
    [Authorize]
    public class TransactionHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionHistoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string status = "", int page = 1, int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bookingsQuery = _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Movie)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Room)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Seat)
                .Where(b => b.UserId == userId);

            // Filter by status if specified
            if (!string.IsNullOrEmpty(status))
            {
                bookingsQuery = bookingsQuery.Where(b => b.Status == status);
            }

            var totalCount = await bookingsQuery.CountAsync();

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new TransactionHistoryViewModel
            {
                Bookings = bookings,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                PageSize = pageSize,
                TotalCount = totalCount,
                StatusFilter = status
            };

            ViewBag.StatusOptions = new List<string> { "", "Pending", "Completed", "Cancelled" };
            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Movie)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Room)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Seat)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch này hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index));
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy booking này." });
            }

            if (booking.Status != "Pending")
            {
                return Json(new { success = false, message = "Chỉ có thể hủy booking đang chờ thanh toán." });
            }

            // Check if showtime is more than 30 minutes away
            var earliestShowTime = booking.BookingDetails.Min(bd => bd.ShowTime.StartTime);
            if (earliestShowTime <= DateTime.Now.AddMinutes(30))
            {
                return Json(new { success = false, message = "Không thể hủy booking khi suất chiếu còn ít hơn 30 phút." });
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy booking thành công." });
        }

        // Export to PDF (optional)
        public async Task<IActionResult> ExportToPdf(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Movie)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Room)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Seat)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null || booking.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Không tìm thấy giao dịch hoặc giao dịch chưa hoàn thành.";
                return RedirectToAction(nameof(Index));
            }

            // Here you can implement PDF generation logic
            // For now, just redirect to details
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}