using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentityEmailProject.Entities;
using MYAcademy_IdentityEmailProject.Models;

namespace MYAcademy_IdentityEmailProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(UserLoginViewModel userLoginViewModel, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(userLoginViewModel.Username) || string.IsNullOrWhiteSpace(userLoginViewModel.Password))
            {
                ViewBag.Error = "Kullanıcı adı ve şifre zorunludur.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                userLoginViewModel.Username,
                userLoginViewModel.Password,
                false,
                false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Inbox", "Message");
            }

            // Şifre doğru ama e-posta 6 haneli kod ile doğrulanmamış
            if (result.IsNotAllowed)
            {
                var user = await _userManager.FindByNameAsync(userLoginViewModel.Username);
                TempData["Error"] = "E-posta adresiniz henüz doğrulanmadı. Lütfen size gönderilen 6 haneli kodu girin.";
                return RedirectToAction("ConfirmMail", "Register", new { email = user?.Email });
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("SignIn");
        }
    }
}
