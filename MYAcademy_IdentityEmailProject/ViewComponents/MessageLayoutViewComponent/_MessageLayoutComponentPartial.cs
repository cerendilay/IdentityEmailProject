using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;

namespace MYAcademy_IdentityEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutHeaderComponentPartial : ViewComponent
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutHeaderComponentPartial(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            var email = user?.Email ?? "";

            // Bildirimler: okunmamış son 5 mesaj
            ViewBag.Notifications = _context.Messages
                .Where(x => x.ReceiverEmail == email && !x.IsRead && !x.IsDraft && !x.IsDeleted)
                .OrderByDescending(x => x.SendDate)
                .Take(5)
                .ToList();

            return View(user);
        }
    }
}
