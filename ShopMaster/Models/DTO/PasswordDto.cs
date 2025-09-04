using System.ComponentModel.DataAnnotations;

namespace ShopMaster.Models.DTO
{
    public class PasswordDto
    {
        [Required(ErrorMessage ="Ce champ est obligatoir")]
        public string CurrentPassword { get; set; }
        [Required(ErrorMessage = "Ce champ est obligatoir")]
        public string NewPassword {  get; set; }
        [Required(ErrorMessage = "Ce champ est obligatoir")]
        [Compare("NewPassword",ErrorMessage ="le mot de passe et le confirm doit etre identique")]
        public string ConfirmPassword { get; set; }
    }
}
