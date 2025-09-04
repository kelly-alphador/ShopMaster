using System.ComponentModel.DataAnnotations;

namespace ShopMaster.Models.DTO
{
    public class LoginDto
    {
        [Required(ErrorMessage ="ce champ est obligatoire")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "ce champ est obligatoire")]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }
}
