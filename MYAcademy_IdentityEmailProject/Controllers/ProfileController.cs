using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Context;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly EmailContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            EmailContext context, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            FillStats(user!.Email!);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(UserEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (model.Image != null)
            {
                var extension = Path.GetExtension(model.Image.FileName).ToLower();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                if (!allowed.Contains(extension))
                    ModelState.AddModelError(nameof(model.Image), "Sadece jpg, png, gif veya webp yükleyebilirsiniz.");
                else if (model.Image.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(nameof(model.Image), "Fotoğraf en fazla 2 MB olabilir.");
            }

            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction("Index");
            }

            user!.Name = model.Name.Trim();
            user.Surname = model.Surname.Trim();
            user.Description = model.Description;

            if (model.Image != null)
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "profile");
                Directory.CreateDirectory(folder);

                // Dosya adını kullanıcıdan almıyoruz, rastgele isim veriyoruz
                var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName).ToLower();
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }
                user.ImageUrl = "/images/profile/" + fileName;
            }

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profil bilgileriniz güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            var result = await _userManager.ChangePasswordAsync(user!, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                TempData["PasswordError"] = string.Join(" ", result.Errors.Select(x => x.Code == "PasswordMismatch" ? "Mevcut şifre hatalı." : x.Description));
                return RedirectToAction("Index");
            }

            // Şifre değişince güvenlik damgası yenilenir, oturumu tazeliyoruz
            await _signInManager.RefreshSignInAsync(user!);
            TempData["Success"] = "Şifreniz başarıyla değiştirildi.";
            return RedirectToAction("Index");
        }

        private void FillStats(string email)
        {
            ViewBag.InboxCount = _context.Messages.Count(x => x.ReceiverEmail == email && !x.IsDraft && !x.IsDeleted);
            ViewBag.SentCount = _context.Messages.Count(x => x.SenderEmail == email && !x.IsDraft);
            ViewBag.StarredCount = _context.Messages.Count(x => x.ReceiverEmail == email && x.IsFavorite && !x.IsDraft && !x.IsDeleted);
            ViewBag.DraftCount = _context.Messages.Count(x => x.SenderEmail == email && x.IsDraft);
        }
    }
}
