using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutSidebarComponentPartial : ViewComponent
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutSidebarComponentPartial(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            var email = user?.Email ?? "";

            // Menü rozetleri veritabanından hesaplanır
            ViewBag.UnreadCount = _context.Messages.Count(x => x.ReceiverEmail == email && !x.IsRead && !x.IsDraft && !x.IsDeleted);
            ViewBag.DraftCount = _context.Messages.Count(x => x.SenderEmail == email && x.IsDraft);
            ViewBag.StarredCount = _context.Messages.Count(x => x.ReceiverEmail == email && x.IsFavorite && !x.IsDraft && !x.IsDeleted);
            ViewBag.TrashCount = _context.Messages.Count(x => x.ReceiverEmail == email && x.IsDeleted && !x.IsDraft);

            var categories = _context.Categories.ToList();
            return View(categories);
        }
    }
}
