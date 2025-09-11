using Azure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PayPal.Api;
using ShopMaster.Context;
using ShopMaster.Models;
using ShopMaster.Models.DTO;
using ShopMaster.Service.Interface;
using ShopMaster.Service.repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace VotreApplication.Controllers
{
    public class PaymentController : Controller
    {
        //on utilise cela pour avoir la config de paypal dans appSetting
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFactureService _factureService;
        public PaymentController(IConfiguration configuration,ApplicationDbContext context, UserManager<ApplicationUser> userManager, IFactureService factureService)
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
            _factureService = factureService;
        }

        // Configuration PayPal
        //on utilise cela pour interagire avec l'application paypal
        private APIContext GetAPIContext()
        {
            var config = new Dictionary<string, string>
            {
                ["mode"] = _configuration["PayPal:Mode"],
                ["clientId"] = _configuration["PayPal:ClientId"],
                ["clientSecret"] = _configuration["PayPal:ClientSecret"]
            };
          //Paypal securise ses api avec avec OAuth2
          //il ne suffit d'utiliser simplement clientID et secetId pour y acceder
          //il faut d'abord avoir un token
            var accessToken = new OAuthTokenCredential(config["clientId"], config["clientSecret"], config).GetAccessToken();
            // APIContext regroupe toutes les infos nécessaires pour appeler PayPal.
            // Il contient l'accessToken pour authentification et la configuration (sandbox/live, logs, timeout, etc.)
            // On utilisera apiContext pour créer, exécuter ou vérifier les paiements.
            var apiContext = new APIContext(accessToken)
            {
                Config = config
            };

            return apiContext;
        }
        //Stocker l'ID de commande en session
        [HttpGet]
        [HttpPost] // Accepte aussi POST
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var cartItems = CartHelper.GetCartItems(Request, Response, _context);

            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "Votre panier est vide";
                return RedirectToAction("Index", "Cart");
            }

            var subtotal = cartItems.Sum(item => item.PrixUnitaire * item.Quantite);
            var shippingFee = subtotal > 50 ? 0 : 5.99m;
            var total = subtotal + shippingFee;

            // 3. Créer une nouvelle commande
            var commande = new Commande
            {
                ClientId = userId, // Id utilisateur connecté
                FraisLivraison = shippingFee,
                AdresseLivraison = user.Adress,
                MethodePaiement = "paypal",
                StatutPaiement = "En attente", 
                StatutCommande = "En attente",
                DateCreation = DateTime.Now,
            };

            _context.Commande.Add(commande);
            _context.SaveChanges(); 

            // Stocker l'ID de la commande en session
            HttpContext.Session.SetString("CommandeId", commande.Id.ToString());

            foreach (var ci in cartItems)
            {
                var produit = await _context.Produit.FindAsync(ci.ProduitId);
                if (produit == null)
                {
                    //  Produit inexistant → ignorer ou lever une erreur
                    continue;
                }

                var ligne = new LigneCommande
                {
                    CommandeId = commande.Id,
                    ProduitId = ci.ProduitId,
                    Quantite = ci.Quantite,
                    PrixUnitaire = ci.PrixUnitaire
                };
                _context.LigneCommande.Add(ligne);
            }
            await _context.SaveChangesAsync();

            ViewBag.CartItems = cartItems;
            ViewBag.Subtotal = subtotal;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.Total = total;

            return View();
        }

        // Créer le paiement PayPal
        [HttpPost]
        public IActionResult CreatePayment(decimal amount, string description = "Achat")
        {
            try
            {
                // Debug : Afficher la configuration
                Console.WriteLine($"[DEBUG] PayPal Mode: {_configuration["PayPal:Mode"]}");
                Console.WriteLine($"[DEBUG] PayPal ClientId: {_configuration["PayPal:ClientId"]?.Substring(0, 10)}...");
                Console.WriteLine($"[DEBUG] Amount: {amount}");

                var apiContext = GetAPIContext();

                // URLs de retour
                var returnUrl = _configuration["PayPal:ReturnUrl"] ?? "https://localhost:7265/Payment/PaymentSuccess";
                var cancelUrl = _configuration["PayPal:CancelUrl"] ?? "https://localhost:7265/Payment/PaymentCancel";

                Console.WriteLine($"[DEBUG] ReturnUrl: {returnUrl}");
                Console.WriteLine($"[DEBUG] CancelUrl: {cancelUrl}");

                // Création de l'objet paiement
                var payment = new Payment
                {
                    //type de payement
                    intent = "sale",
                    //comment le client va payer
                    payer = new Payer { payment_method = "paypal" },
                    transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            description = description,
                            invoice_number = Guid.NewGuid().ToString(),
                            //Spécifie la monnaie et le montant à paye
                            amount = new Amount
                            {
                                //la devise (Euro ,dollar)
                                currency = _configuration["PayPal:Currency"] ?? "EUR",
                                total = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            }
                        }
                    },
                    redirect_urls = new RedirectUrls
                    {
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    }
                };

                Console.WriteLine($"[DEBUG] Payment object created successfully");

                // Créer le paiement
                var createdPayment = payment.Create(apiContext);

                Console.WriteLine($"[DEBUG] Payment created with ID: {createdPayment.id}");

                // Trouver l'URL d'approbation
                var approvalUrl = createdPayment.links
                    .FirstOrDefault(x => x.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;

                if (string.IsNullOrEmpty(approvalUrl))
                {
                    Console.WriteLine("[ERROR] No approval URL found");
                    ViewBag.Error = "Erreur lors de la création du paiement PayPal";
                    return View("Error");
                }

                Console.WriteLine($"[DEBUG] Approval URL: {approvalUrl}");

                // Stocker l'ID du paiement en session (ou en base de données)
                HttpContext.Session.SetString("PaymentId", createdPayment.id);

                // Rediriger vers PayPal
                return Redirect(approvalUrl);
            }
            catch (PayPal.PayPalException paypalEx)
            {
                Console.WriteLine($"[ERROR] PayPal Exception: {paypalEx.Message}");
                Console.WriteLine($"[ERROR] PayPal Details: {paypalEx.Message}");
                ViewBag.Error = $"Erreur PayPal spécifique: {paypalEx.Message} - {paypalEx.Message}";
                return View("Error");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] General Exception: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                ViewBag.Error = $"Erreur PayPal: {ex.Message}";
                return View("Error");
            }
        }
        // Action appelée depuis votre page de checkout
        [HttpPost]
        public IActionResult ProcessPayPalPayment()
        {
            try
            {
                // Récupérer les informations du panier depuis la session ou la base de données
                var cartItems = CartHelper.GetCartItems(Request, Response, _context); // Vous devez implémenter cette méthode
                var total = CalculateTotal(cartItems); // Vous devez implémenter cette méthode

                if (cartItems == null || !cartItems.Any())
                {
                    TempData["Error"] = "Votre panier est vide";
                    return RedirectToAction("Index", "Cart");
                }

                return CreatePayment(total, "Commande ShopMaster");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors du traitement: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        //  CORRECTION dans PaymentSuccess - Maintenant on peut récupérer l'ID de commande
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string paymentId, string PayerID)
        {
            try
            {
                var apiContext = GetAPIContext();

                // Récupérer l'ID du paiement depuis la session
                var storedPaymentId = HttpContext.Session.GetString("PaymentId");
                if (string.IsNullOrEmpty(storedPaymentId) || storedPaymentId != paymentId)
                {
                    ViewBag.Error = "ID de paiement invalide";
                    return View("Error");
                }

                //  Récupérer l'ID de la commande depuis la session
                var commandeIdString = HttpContext.Session.GetString("CommandeId");
                if (!int.TryParse(commandeIdString, out int commandeId))
                {
                    ViewBag.Error = "Commande introuvable";
                    return View("Error");
                }

                // Exécuter le paiement PayPal
                var paymentExecution = new PaymentExecution { payer_id = PayerID };
                var payment = new Payment { id = paymentId };
                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() == "approved")
                {
                    // ✅ AJOUT : Mettre à jour le statut de la commande à "Payé"
                    var commande = await _context.Commande.FindAsync(commandeId);
                    if (commande != null)
                    {
                        commande.StatutPaiement = "Payé";
                        commande.StatutCommande = "Payé";
                        commande.DateCreation = DateTime.Now; // Si vous avez ce champ
                        await _context.SaveChangesAsync();
                    }

                    // Récupérer l'utilisateur connecté
                    var user = await _userManager.GetUserAsync(User);

                    // ENVOYER LA FACTURE PAR EMAIL
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        try
                        {
                            await _factureService.EnvoyerFactureParEmailAsync(commandeId, user.Email);
                            TempData["InvoiceSuccess"] = " Paiement réussi ! La facture a été envoyée à votre adresse email.";
                        }
                        catch (Exception emailEx)
                        {
                            // Log l'erreur mais ne pas faire échouer la transaction
                            TempData["InvoiceWarning"] = " Paiement réussi mais erreur lors de l'envoi de la facture. Contactez le support.";
                        }
                    }
                    else
                    {
                        TempData["InvoiceWarning"] = " Paiement réussi mais impossible d'envoyer la facture (email non trouvé).";
                    }

                    // Récupérer les détails de la transaction pour l'affichage
                    var transaction = executedPayment.transactions.FirstOrDefault();

                    // Préparer les données pour la vue
                    ViewBag.PaymentId = paymentId;
                    ViewBag.PayerId = PayerID;
                    ViewBag.Amount = transaction?.amount?.total;
                    ViewBag.Currency = transaction?.amount?.currency;
                    ViewBag.TransactionId = transaction?.related_resources?.FirstOrDefault()?.sale?.id;
                    ViewBag.CommandeId = commandeId;
                    ViewBag.UserEmail = user?.Email;

                    // Nettoyer les données temporaires
                    HttpContext.Session.Remove("PaymentId");
                    HttpContext.Session.Remove("CommandeId");
                    Response.Cookies.Delete("shopping_cart"); // Vider le panier après commande réussie

                    return View("PaymentSuccess");
                }
                else
                {
                    ViewBag.Error = " Le paiement n'a pas été approuvé par PayPal";
                    return View("Error");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $" Erreur lors de l'exécution du paiement: {ex.Message}";
                return View("Error");
            }
        }



       
        [HttpGet]
        public IActionResult PaymentCancel()
        {
            // ✅ AJOUT : Nettoyer aussi l'ID de commande
            HttpContext.Session.Remove("PaymentId");
            HttpContext.Session.Remove("CommandeId");

            ViewBag.Message = "Le paiement a été annulé";
            return View("PaymentCancel");
        }
        // Webhook PayPal (optionnel)
        [HttpPost]
        public IActionResult Webhook()
        {
            try
            {
                // Ici vous pouvez traiter les notifications PayPal
                // Vérifier la signature du webhook
                // Mettre à jour le statut des paiements

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur webhook: {ex.Message}");
            }
        }

        // Méthode utilitaire pour obtenir les détails d'un paiement
        [HttpGet]
        public IActionResult GetPaymentDetails(string paymentId)
        {
            try
            {
                var apiContext = GetAPIContext();
                var payment = Payment.Get(apiContext, paymentId);

                return Json(new
                {
                    id = payment.id,
                    state = payment.state,
                    amount = payment.transactions.FirstOrDefault()?.amount?.total,
                    currency = payment.transactions.FirstOrDefault()?.amount?.currency,
                    createTime = payment.create_time
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Méthodes utilitaires utilisant votre CartHelper
        private decimal CalculateTotal(List<LigneCommande> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return 0;

            decimal subtotal = CartHelper.GetSubtotal(cartItems);
            decimal shippingFee = subtotal > 50 ? 0 : 5.99m; // Exemple: livraison gratuite > 50€
            return subtotal + shippingFee;
        }

        // Page d'erreur
        public IActionResult Error()
        {
            return View();
        }
    }
}