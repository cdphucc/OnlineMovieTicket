using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OnlineMovieTicket.Data;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Services;

namespace OnlineMovieTicket.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVietQRService _vietQrService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public BookingsController(ApplicationDbContext context, IVietQRService vietQrService, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _vietQrService = vietQrService;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult SelectSeat(int showTimeId)
        {
            var movieTitle = _context.Movies
                .Where(m => m.ShowTimes.Any(st => st.Id == showTimeId))
                .Select(m => m.Title)
                .FirstOrDefault();

            var showTime = _context.ShowTimes
                .Include(st => st.Movie)
                .FirstOrDefault(st => st.Id == showTimeId);
            if (showTime == null) return NotFound();

            var bookedSeatIds = _context.BookingDetails
                .Where(bd => bd.ShowTimeId == showTimeId && bd.Booking.Status == "Completed")
                .Select(bd => bd.SeatId)
                .ToList();

            var seats = _context.Seats.ToList();

            ViewBag.ShowTime = showTime;
            ViewBag.BookedSeatIds = bookedSeatIds;
            ViewBag.Seats = seats;
            //thông tin phim
            ViewBag.movieTitle = movieTitle;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(int showTimeId, List<int> seatIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Bạn cần đăng nhập để đặt vé!" });

            if (seatIds == null || seatIds.Count == 0 || seatIds.Count > 5)
                return Json(new { success = false, message = "Bạn chỉ được chọn từ 1 đến 5 ghế!" });

            var showTime = _context.ShowTimes.FirstOrDefault(st => st.Id == showTimeId);
            if (showTime == null)
                return Json(new { success = false, message = "Suất chiếu không tồn tại!" });

            var bookedSeatIds = _context.BookingDetails
                .Where(bd => bd.ShowTimeId == showTimeId && seatIds.Contains(bd.SeatId) && bd.Booking.Status == "Completed")
                .Select(bd => bd.SeatId)
                .ToList();

            if (bookedSeatIds.Any())
                return Json(new { success = false, message = "Có ghế đã bị người khác đặt, vui lòng chọn lại!" });

            decimal total = seatIds.Count * showTime.Price;

            var booking = new Booking
            {
                UserId = userId,
                BookingTime = DateTime.Now,
                TotalAmount = total,
                Status = "Pending", // Chưa thanh toán
                CreatedAt = DateTime.Now,
                BookingDetails = seatIds.Select(seatId => new BookingDetail
                {
                    ShowTimeId = showTimeId,
                    SeatId = seatId
                }).ToList()
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return Json(new { success = true, redirectUrl = Url.Action("PaymentQR", new { bookingId = booking.Id }) });
        }

        public async Task<IActionResult> PaymentQR(int bookingId)
        {
            try
            {
                Console.WriteLine($"=== PAYMENT QR DEBUG ===");
                Console.WriteLine($"BookingId: {bookingId}");

                // Load booking đơn giản trước
                var booking = await _context.Bookings
                    .Include(b => b.BookingDetails)
                        .ThenInclude(d => d.ShowTime)
                            .ThenInclude(st => st.Movie)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                Console.WriteLine($"Booking found: {booking != null}");

                if (booking == null)
                {
                    Console.WriteLine("❌ Booking not found");
                    TempData["ErrorMessage"] = "Không tìm thấy đơn đặt vé!";
                    return RedirectToAction("Index", "Home");
                }

                // Kiểm tra nếu booking đã bị hủy
                if (booking.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = "Đơn đặt vé này đã bị hủy!";
                    return RedirectToAction("Index", "Home");
                }

                // Debug để xem có thành công không
                Console.WriteLine($"✅ Booking loaded successfully");
                Console.WriteLine($"Status: {booking.Status}");
                Console.WriteLine($"Total: {booking.TotalAmount}");

                // Tạo QR request đơn giản
                var qrRequest = new VietQRRequestModel
                {
                    BookingId = bookingId,
                    AccountNo = _configuration["VietQR:AccountNo"],
                    AccountName = _configuration["VietQR:AccountName"],
                    BankId = _configuration["VietQR:BankId"],
                    Amount = booking.TotalAmount,
                    Description = $"Thanh toan ve xem phim - Don hang #{bookingId}",
                    Template = "compact2"
                };

                var qrResult = await _vietQrService.GenerateQRCodeAsync(qrRequest);

                Console.WriteLine($"QR Result Success: {qrResult?.Success}");

                // Set ViewBag
                ViewBag.Booking = booking;
                ViewBag.QRResult = qrResult;

                return View(qrResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // Thêm action để hủy giao dịch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTransaction(int bookingId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var booking = await _context.Bookings
                    .Include(b => b.BookingDetails)
                    .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId && b.Status == "Pending");

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn đặt vé hoặc bạn không có quyền hủy!";
                    return RedirectToAction("Index", "Home");
                }

                // Cập nhật trạng thái booking thành "Cancelled"
                booking.Status = "Cancelled";
                booking.CancelledAt = DateTime.Now; // Nếu có trường này trong model

                // Xóa các booking details để giải phóng ghế
                _context.BookingDetails.RemoveRange(booking.BookingDetails);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã hủy giao dịch thành công. Ghế đã được giải phóng.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy giao dịch: {ex.Message}";
                return RedirectToAction("PaymentQR", new { bookingId = bookingId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmQRPayment(int bookingId)
        {
            try
            {
                var booking = _context.Bookings
                    .Include(b => b.User)
                    .FirstOrDefault(b => b.Id == bookingId && b.Status == "Pending");
                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn đặt vé hoặc đơn đã được xử lý!";
                    return RedirectToAction("SelectSeat", "Bookings");
                }
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    Amount = booking.TotalAmount,
                    PaymentMethod = "VietQR",
                    TransactionId = $"QR_{DateTime.Now:yyyyMMddHHmmss}_{booking.Id}",
                    Status = "Completed",
                    PaymentDate = DateTime.Now,
                    BankAccountNo = _configuration["VietQR:AccountNo"],
                    BankName = _configuration["VietQR:BankName"]
                };
                booking.Status = "Completed"; // Cập nhật trạng thái đặt vé
                _context.Payments.Add(payment);
                _context.SaveChanges();
                try
                {
                    var user = booking.User.Email;

                    if (!string.IsNullOrEmpty(booking.User.Email))
                    {
                        _emailService.SendInvoiceEmailAsync(booking.User.Email, booking.User.FullName ?? booking.User.Email, booking);
                    }
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the payment
                    Console.WriteLine($"Error sending invoice email: {emailEx.Message}");
                }
                TempData["SuccessMessage"] = "Thanh toán thành công!";
                return RedirectToAction("Invoice", new { bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xác nhận thanh toán: {ex.Message}";
                return RedirectToAction("PaymentQR", new { bookingId = bookingId });
            }
        }

        [HttpGet]
        public IActionResult Invoice(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Seat)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.ShowTime)
                        .ThenInclude(st => st.Movie)
                .FirstOrDefault(b => b.Id == bookingId);
            if (booking == null || booking.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) || booking.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn!";
                return RedirectToAction("Index", "Home");
            }
            return View(booking);
        }

        // GET: Bookings/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Set<ApplicationUser>(), "Id", "Id");
            return View();
        }

        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,BookingTime,TotalAmount,Status,CreatedAt,PaymentId")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Set<ApplicationUser>(), "Id", "Id", booking.UserId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Set<ApplicationUser>(), "Id", "Id", booking.UserId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,BookingTime,TotalAmount,Status,CreatedAt,PaymentId")] Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Set<ApplicationUser>(), "Id", "Id", booking.UserId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}