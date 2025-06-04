using System;
using System.Collections.Generic;
using System.Linq;
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
        // Controller for managing bookings in the online movie ticketing system
        private readonly ApplicationDbContext _context;
        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult SelectCinema()
        {
            var cinemas = _context.Cinemas.ToList();
            return View(cinemas);
        }

        public IActionResult SelectMovie(int cinemaId)
        {
            var movies = _context.ShowTimes
                .Where(st => st.Room.CinemaId == cinemaId && st.StartTime >= DateTime.Now)
                .Select(st => st.Movie)
                .Distinct()
                .ToList();
            ViewBag.CinemaId = cinemaId;
            return View(movies);
        }
        public IActionResult SelectShowTime(int movieId)
        {
            var showtimes = _context.ShowTimes
                .Include(st => st.Room)
                    .ThenInclude(r => r.Seats)
                .Where(st => st.MovieId == movieId && st.StartTime >= DateTime.Now)
                .ToList();

            if (!showtimes.Any())
            {
                ViewBag.Message = "Hiện tại không có suất chiếu cho phim này.";
                return View("NoShowTime");
            }

            ViewBag.MovieId = movieId;
            return View(showtimes);
        }
        public IActionResult SelectSeat(int showTimeId)
        {
            var showtime = _context.ShowTimes
                .Include(st => st.Room)
                .ThenInclude(r => r.Seats)
                .Include(st => st.BookingDetails)
                .FirstOrDefault(st => st.Id == showTimeId);

            if (showtime == null)
            {
                return NotFound("Suất chiếu không tồn tại.");
            }

            var bookedSeatIds = showtime.BookingDetails?
                .Where(bd => bd.Status == "Booked")
                .Select(bd => bd.SeatId)
                .ToHashSet();

            ViewBag.ShowTime = showtime;
            ViewBag.BookedSeatIds = bookedSeatIds ?? new HashSet<int>();
            return View(showtime.Room.Seats.ToList());
        }

        public IActionResult Payment(int showTimeId, List<int> selectedSeats)
        {
            var userId = User.Identity.Name;
            if (string.IsNullOrEmpty(userId) || !_context.Users.Any(u => u.Id == userId))
            {
                return BadRequest("Người dùng không tồn tại hoặc chưa đăng nhập.");
            }

            var showtime = _context.ShowTimes.FirstOrDefault(st => st.Id == showTimeId);
            if (showtime == null) return NotFound();

            var booking = new Booking
            {
                UserId = userId,
                BookingTime = DateTime.Now,
                TotalAmount = 0,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                PromotionId = 0,
                PaymentId = 0,
                BookingDetails = new List<BookingDetail>()
            };

            foreach (var seatId in selectedSeats)
            {
                booking.BookingDetails.Add(new BookingDetail
                {
                    ShowTimeId = showTimeId,
                    SeatId = seatId,
                    Price = showtime.Price,
                    Status = "Booked"
                });
            }

            booking.TotalAmount = booking.BookingDetails.Sum(bd => bd.Price);
            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return RedirectToAction("ProcessPayment", new { bookingId = booking.Id });
        }

        public IActionResult ProcessPayment(int bookingId)
        {
            var booking = _context.Bookings.Include(b => b.BookingDetails).FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return NotFound();

            return View(booking);
        }
        // GET: Bookings
        // public async Task<IActionResult> Index()
        // {
        //   return View(await _context.Bookings.ToListAsync());
        // }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
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
        public async Task<IActionResult> Create([Bind("Id,UserId,BookingTime,TotalAmount,Status,PromotionId,PaymentId,CreatedAt")] Booking booking)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,BookingTime,TotalAmount,Status,PromotionId,PaymentId,CreatedAt")] Booking booking)
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
