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

        // GET: /Booking/SelectSeat?showTimeId=#
        public IActionResult SelectSeat(int showTimeId)
        {
            var showTime = _context.ShowTimes
                .Include(s => s.Room)
                .FirstOrDefault(s => s.Id == showTimeId);
            if (showTime == null) return NotFound();

            var seats = _context.Seats
                .Where(s => s.RoomId == showTime.RoomId)
                .ToList();

            var bookedSeatIds = _context.BookingDetails
                .Where(bd => bd.ShowTimeId == showTimeId && bd.Booking.Status == "Active")
                .Select(bd => bd.SeatId)
                .ToHashSet();

            foreach (var seat in seats)
                seat.Status = bookedSeatIds.Contains(seat.Id) ? "Booked" : "Available";

            ViewBag.ShowTimeId = showTimeId;
            ViewBag.SeatPrice = showTime.Price;
            return View(seats);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(int showTimeId, List<int> seatIds)
        {
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            decimal total = seatIds.Count * showTime.Price;

            var booking = new Booking
            {
                UserId = userId,
                BookingTime = DateTime.Now,
                TotalAmount = total,
                Status = "Active",
                CreatedAt = DateTime.Now,
                BookingDetails = seatIds.Select(seatId => new BookingDetail
                {
                    ShowTimeId = showTimeId,
                    SeatId = seatId
                }).ToList()
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return Json(new { success = true });
        }
    }
}
