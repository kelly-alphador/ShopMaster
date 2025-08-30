using System.ComponentModel.DataAnnotations;

namespace ShopMaster.Models.DTO
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [MaxLength(25)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Phone]
        public string? Tel { get; set; }
        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [MaxLength(25)]
        public string Adress {  get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        [MaxLength(100)] // 50 caractères peuvent être insuffisants pour certains mots de passe
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = ""; // Correction de la casse
    }
}
