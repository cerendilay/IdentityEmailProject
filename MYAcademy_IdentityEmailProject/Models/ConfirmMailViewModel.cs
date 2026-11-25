using System.ComponentModel.DataAnnotations;

namespace MYAcademy_IdentityEmailProject.Models
{
    public class ConfirmMailViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [Range(100000, 999999, ErrorMessage = "Doğrulama kodu 6 haneli olmalıdır.")]
        public int? ConfirmCode { get; set; }
    }
}
