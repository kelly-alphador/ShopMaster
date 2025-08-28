using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShopMaster.Context;
using ShopMaster.Models;

namespace ShopMaster.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Charger TOUS les produits au lieu de seulement 4
            var produits = _context.Produit.OrderByDescending(p => p.Id).ToList();
            return View(produits);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
