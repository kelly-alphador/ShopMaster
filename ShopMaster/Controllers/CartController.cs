using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ShopMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using PayPal;
using ShopMaster.Service.repos;
using ShopMaster.Context;
using ShopMaster.Models.DTO;
using System.Security.Claims; // Ajoutez ce using pour PayPalException

namespace ShopMaster.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly PayPalService _payPalService;
        private readonly ApplicationDbContext _context;
        private const decimal shippingFee = 5.99m;

        public CartController(ILogger<CartController> logger, PayPalService payPalService, ApplicationDbContext context)
        {
            _logger = logger;
            _payPalService = payPalService;
            _context = context;
        }

        public IActionResult Index()
        {
            List<LigneCommande> cartItems = CartHelper.GetCartItems(Request, Response, _context);
            decimal subtotal = CartHelper.GetSubtotal(cartItems);
            ViewBag.CartItems = cartItems;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.Subtotal = subtotal;
            ViewBag.Total = subtotal + shippingFee;
            return View();
        }

        //  pour vider complètement le panier
        [HttpPost]
        public IActionResult Clear()
        {
            Response.Cookies.Delete("shopping_cart");
            return RedirectToAction("Index");
        }

       


        /*[Authorize]
        public IActionResult OrderConfirmation(int id)
        {
            // Récupérer la commande depuis la base de données avec les détails
            var order = context.Commande
                .Where(c => c.Id == id && c.ClientId == userManager.GetUserAsync(User).Result.Id)
                .Select(c => new Commande
                {
                    Id = c.Id,
                    DateCreation = c.DateCreation,
                    StatutCommande = c.StatutCommande,
                    StatutPaiement = c.StatutPaiement,
                    FraisLivraison = c.FraisLivraison,
                    MethodePaiement = c.MethodePaiement,
                    AdresseLivraison = c.AdresseLivraison,
                    LignesCommande = c.LignesCommande.Select(lc => new LigneCommande
                    {
                        PrixUnitaire = lc.PrixUnitaire,
                        Quantite = lc.Quantite,
                        Produit = context.Produit.FirstOrDefault(p => p.Id == lc.ProduitId)
                    }).ToList()
                })
                .FirstOrDefault();

            if (order == null)
            {
                return NotFound("Commande introuvable.");
            }

            return View(order);
        }*/

    }
}
