using Microsoft.AspNetCore.Identity;

namespace ShopMaster.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string Nom { get; set; } = "";
        public string prenom { get; set; } = "";
        public string Adress { get; set; } = "";
        public DateTime DateCreation { get; set; }
    }
}
