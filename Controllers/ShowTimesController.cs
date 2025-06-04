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
    public class ShowTimesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShowTimesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetByMovie(int movieId)
        {
            var now = DateTime.Now;

            // 1. Tìm các suất chiếu đã qua nhưng vẫn "Available"
            var expiredShowtimes = _context.ShowTimes
                .Where(st => st.MovieId == movieId && st.Status == "Available" && st.StartTime < now)
                .ToList();

            // 2. Đổi status thành "Expired"
            foreach (var st in expiredShowtimes)
            {
                st.Status = "Expired";
            }
            if (expiredShowtimes.Count > 0)
                _context.SaveChanges();

            // 3. Lấy các suất chiếu còn khả dụng (chỉ trả về future showtimes)
            var showtimes = _context.ShowTimes
                .Where(st => st.MovieId == movieId && st.Status == "Available" && st.StartTime > now)
                .OrderBy(st => st.StartTime)
                .Select(st => new
                {
                    id = st.Id,
                    startTime = st.StartTime.ToString("dd/MM/yyyy HH:mm"),
                    roomName = st.Room.Name
                })
                .ToList();

            return Json(showtimes);
        }

        // GET: ShowTimes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ShowTimes.Include(s => s.Movie).Include(s => s.Room);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ShowTimes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var showTime = await _context.ShowTimes
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (showTime == null)
            {
                return NotFound();
            }

            return View(showTime);
        }

        // GET: ShowTimes/Create
        public IActionResult Create()
        {
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Id");
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id");
            return View();
        }

        // POST: ShowTimes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MovieId,RoomId,StartTime,Price,Status,Format")] ShowTime showTime)
        {
            if (ModelState.IsValid)
            {
                _context.Add(showTime);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Id", showTime.MovieId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id", showTime.RoomId);
            return View(showTime);
        }

        // GET: ShowTimes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var showTime = await _context.ShowTimes.FindAsync(id);
            if (showTime == null)
            {
                return NotFound();
            }
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Id", showTime.MovieId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id", showTime.RoomId);
            return View(showTime);
        }

        // POST: ShowTimes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MovieId,RoomId,StartTime,Price,Status,Format")] ShowTime showTime)
        {
            if (id != showTime.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(showTime);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShowTimeExists(showTime.Id))
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
            ViewData["MovieId"] = new SelectList(_context.Movies, "Id", "Id", showTime.MovieId);
            ViewData["RoomId"] = new SelectList(_context.Rooms, "Id", "Id", showTime.RoomId);
            return View(showTime);
        }

        // GET: ShowTimes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var showTime = await _context.ShowTimes
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (showTime == null)
            {
                return NotFound();
            }

            return View(showTime);
        }

        // POST: ShowTimes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var showTime = await _context.ShowTimes.FindAsync(id);
            if (showTime != null)
            {
                _context.ShowTimes.Remove(showTime);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShowTimeExists(int id)
        {
            return _context.ShowTimes.Any(e => e.Id == id);
        }
    }
}
