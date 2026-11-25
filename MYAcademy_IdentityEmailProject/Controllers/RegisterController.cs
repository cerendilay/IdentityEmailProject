using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;
using MYAcademy_IdentityEmailProject.Services;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly MailService _mailService;

        public RegisterController(UserManager<AppUser> userManager, MailService mailService)
        {
            _userManager = userManager;
            _mailService = mailService;
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(UserRegisterViewModel userRegisterViewModel)
        {
            Random random = new Random();
            int validCode = random.Next(100000, 1000000);

            AppUser appUser = new AppUser()
            {
                Name = userRegisterViewModel.Name,
                Surname = userRegisterViewModel.Surname,
                Email = userRegisterViewModel.Email,
                UserName = userRegisterViewModel.Username,
                ValidCode = validCode
            };

            var result = await _userManager.CreateAsync(appUser, userRegisterViewModel.Password);

            if (result.Succeeded)
            {
                try
                {
                    _mailService.SendMail(userRegisterViewModel.Email,
                        "Email Doğrulama Kodu",
                        "Sisteme giriş yapabilmek için gerekli olan doğrulama kodu: " + validCode);
                }
                catch
                {
                    TempData["Error"] = "Doğrulama kodu e-postası gönderilemedi. 'Kodu tekrar gönder' ile yeniden deneyebilirsiniz.";
                }

                // Kayıt sonrası 6 haneli kodun girileceği sayfaya yönlendir
                return RedirectToAction("ConfirmMail", new { email = userRegisterViewModel.Email });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(userRegisterViewModel);
            }
        }

        [HttpGet]
        public IActionResult ConfirmMail(string? email)
        {
            return View(new ConfirmMailViewModel { Email = email ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmMail(ConfirmMailViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || user.ValidCode == null || user.ValidCode != model.ConfirmCode)
            {
                ModelState.AddModelError("", "Doğrulama kodu hatalı.");
                return View(model);
            }

            user.EmailConfirmed = true;
            user.ValidCode = null;   // kod tek kullanımlık
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "E-posta adresiniz doğrulandı. Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("SignIn", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode(string email)
        {
            var user = await _userManager.FindByEmailAsync(email ?? "");

            if (user != null && !user.EmailConfirmed)
            {
                user.ValidCode = new Random().Next(100000, 1000000);
                await _userManager.UpdateAsync(user);

                try
                {
                    _mailService.SendMail(user.Email!,
                        "Email Doğrulama Kodu",
                        "Sisteme giriş yapabilmek için gerekli olan doğrulama kodu: " + user.ValidCode);
                }
                catch
                {
                    TempData["Error"] = "E-posta gönderilemedi, lütfen daha sonra tekrar deneyin.";
                    return RedirectToAction("ConfirmMail", new { email });
                }
            }

            TempData["Success"] = "Hesap doğrulanmamışsa yeni kod e-posta adresinize gönderildi.";
            return RedirectToAction("ConfirmMail", new { email });
        }
    }
}
