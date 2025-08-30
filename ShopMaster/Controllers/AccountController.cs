using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopMaster.Models;

namespace ShopMaster.Controllers
{
    public class AccountController : Controller
    {
        //userManagger c'est gestionnaire de l'utilisateur il sert a creer , modifier chercher etc c'est qui concerne l'user
        private readonly UserManager<ApplicationUser> _userManager;
        //gestionnaire de connexion 
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
    }
}
