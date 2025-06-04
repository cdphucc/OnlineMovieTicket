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

namespace OnlineMovieTicket.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult SelectSeat(int showTimeId)
        {
            var showTime = _context.ShowTimes
                .Include(st => st.Movie)
                .FirstOrDefault(st => st.Id == showTimeId);
            if (showTime == null) return NotFound();

            var bookedSeatIds = _context.BookingDetails
                .Where(bd => bd.ShowTimeId == showTimeId && bd.Booking.Status == "Active")
                .Select(bd => bd.SeatId)
                .ToList();

            var seats = _context.Seats.ToList();

            ViewBag.ShowTime = showTime;
            ViewBag.BookedSeatIds = bookedSeatIds;
            ViewBag.Seats = seats;
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
                .Where(bd => bd.ShowTimeId == showTimeId && seatIds.Contains(bd.SeatId) && bd.Booking.Status == "Active")
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

            return Json(new { success = true, redirectUrl = Url.Action("Payment", new { bookingId = booking.Id }) });
        }

        [HttpGet]
        public IActionResult Payment(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Seat)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.ShowTime)
                .FirstOrDefault(b => b.Id == bookingId);

            if (booking == null || booking.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) || booking.Status != "Pending")
            {
                return RedirectToAction("SelectSeat", "Bookings");
            }
            return View(booking); // <- phải có model truyền vào đây
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPayment(int bookingId)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);

            if (booking == null || booking.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) || booking.Status != "Pending")
            {
                return RedirectToAction("SelectSeat", "Bookings");
            }
            booking.Status = "Active";
            _context.SaveChanges();

            return RedirectToAction("Confirmation", new { bookingId = bookingId });
        }

        [HttpGet]
        public IActionResult Confirmation(int bookingId)
        {
            var booking = _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Seat)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.ShowTime)
                .FirstOrDefault(b => b.Id == bookingId);

            if (booking == null || booking.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) || booking.Status != "Active")
            {
                return RedirectToAction("SelectSeat", "Bookings");
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
