using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;
using System.Text;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class PasswordController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly MailService _mailService;

        public PasswordController(UserManager<AppUser> userManager, MailService mailService)
        {
            _userManager = userManager;
            _mailService = mailService;
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                // Identity token'ı; şifre değişince SecurityStamp değiştiği için tek kullanımlıktır,
                // süresi Program.cs'de 1 saat olarak ayarlandı.
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var link = Url.Action("ResetPassword", "Password",
                    new { userId = user.Id, token = encodedToken }, Request.Scheme);

                try
                {
                    _mailService.SendMail(user.Email!, "Şifre Yenileme",
                        $"Merhaba {user.Name},<br/><br/>Şifrenizi yenilemek için aşağıdaki bağlantıya tıklayın (1 saat geçerlidir):<br/>" +
                        $"<a href=\"{link}\">Şifremi Yenile</a><br/><br/>Bu isteği siz yapmadıysanız bu e-postayı dikkate almayın.",
                        isHtml: true);
                }
                catch
                {
                    ModelState.AddModelError("", "E-posta gönderilemedi, lütfen daha sonra tekrar deneyin.");
                    return View(model);
                }
            }

            // Güvenlik: e-postanın sistemde olup olmadığını belli etmiyoruz
            TempData["Success"] = "Bu e-posta adresi sistemde kayıtlıysa şifre yenileme bağlantısı gönderildi.";
            return RedirectToAction("ForgotPassword");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? userId, string? token)
        {
            var model = new ResetPasswordViewModel { UserId = userId ?? "", Token = token ?? "" };

            var user = userId == null ? null : await _userManager.FindByIdAsync(userId);
            var decodedToken = DecodeToken(token);

            bool isValid = user != null && decodedToken != null &&
                await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider,
                    UserManager<AppUser>.ResetPasswordTokenPurpose, decodedToken);

            ViewBag.InvalidLink = !isValid;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            var decodedToken = DecodeToken(model.Token);

            if (user == null || decodedToken == null)
            {
                ViewBag.InvalidLink = true;
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

            if (result.Succeeded)
            {
                TempData["Success"] = "Şifreniz güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
                return RedirectToAction("SignIn", "Login");
            }

            if (result.Errors.Any(x => x.Code == "InvalidToken"))
            {
                ViewBag.InvalidLink = true;
                return View(model);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        private static string? DecodeToken(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch
            {
                return null;
            }
        }
    }
}
