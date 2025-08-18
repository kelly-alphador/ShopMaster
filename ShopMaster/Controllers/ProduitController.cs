using Microsoft.AspNetCore.Mvc;
using ShopMaster.Context;

namespace ShopMaster.Controllers
{
    public class ProduitController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProduitController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var produits=_context.Produit.ToList();
            return View(produits);
        }
    }
}
