using System.ComponentModel.DataAnnotations;

namespace MYAcademy_IdentityEmailProject.Models
{
    public class UserEditViewModel
    {
        [Required(ErrorMessage = "Ad zorunludur.")]
        [StringLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [StringLength(50)]
        public string Surname { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }

        public IFormFile? Image { get; set; }
    }
}
