using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;
using ShopMaster.Models;

namespace ShopMaster.Context
{
    public class DatabaseInitializer
    {
        public static async Task AmmorchageDonne(UserManager<ApplicationUser>? userManager, RoleManager<IdentityRole>? roleManager)
        {
            //verifie si le userManager et roleManager est null
            if (userManager == null || roleManager == null)
            {
                Console.WriteLine("user manager et roleManager est null");
                return;
            }
            //verifie si le role n'exite pas si il n'existe pas on le creer 
            var exist = await roleManager.RoleExistsAsync("Admin");
            if (!exist)
            {
                Console.WriteLine("on va creer le role admin");
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            exist = await roleManager.RoleExistsAsync("Vender");
            if (!exist) 
            {
                Console.WriteLine("on va creer le role Vender");
                await roleManager.CreateAsync(new IdentityRole("Vender"));
            }
            exist = await roleManager.RoleExistsAsync("Client");
            if (!exist) 
            {
                Console.WriteLine("on va creer le role client");
                await roleManager.CreateAsync(new IdentityRole("Client"));
            }
            //on va creer une variable et on va le verifier si il est vide 
            var adminuser = await userManager.GetUsersInRoleAsync("Admin");
            if (adminuser.Any()) 
            {
                //Il a au moin un admin 
                Console.WriteLine("il y a deja un admin");
            }
            var user = new ApplicationUser
            {
                Nom = "Admin",
                UserName="Admin",
                prenom = "Admin",
                Email="Admin@gmail.com",
                Adress = "Tsaramandroso",
                DateCreation = DateTime.Now,
            };
            var initialPassword = "Admin@1234";
            //ajout de utilisateur admin
            var result=await userManager.CreateAsync(user,initialPassword);
            if (result.Succeeded) 
            {
                //Ajout de role 
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }
}
