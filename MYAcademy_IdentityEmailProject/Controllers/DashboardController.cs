using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var email = user!.Email!;

            // Tüm sayılar veritabanındaki gerçek verilerden hesaplanır
            ViewBag.InboxCount = _context.Messages.Count(x => x.ReceiverEmail == email && !x.IsDraft && !x.IsDeleted);
            ViewBag.SentCount = _context.Messages.Count(x => x.SenderEmail == email && !x.IsDraft);
            ViewBag.UnreadCount = _context.Messages.Count(x => x.ReceiverEmail == email && !x.IsRead && !x.IsDraft && !x.IsDeleted);
            ViewBag.StarredCount = _context.Messages.Count(x => x.ReceiverEmail == email && x.IsFavorite && !x.IsDraft && !x.IsDeleted);
            ViewBag.DraftCount = _context.Messages.Count(x => x.SenderEmail == email && x.IsDraft);

            // Son gelen 5 mesaj
            ViewBag.LastMessages = await _context.Messages
                .Where(x => x.ReceiverEmail == email && !x.IsDraft && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .Take(5)
                .ToListAsync();

            // Gelen kutusundaki mesajların kategorilere göre dağılımı (grafik için)
            ViewBag.CategoryStats = await _context.Messages
                .Where(x => x.ReceiverEmail == email && !x.IsDraft && !x.IsDeleted && x.CategoryId != null)
                .GroupBy(x => x.Category!.CategoryName)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .ToListAsync();

            return View(user);
        }
    }
}
