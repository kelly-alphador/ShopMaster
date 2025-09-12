using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMaster.Context;
using ShopMaster.Models;

namespace ShopMaster.Controllers
{
    public class TableauDeBordController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TableauDeBordController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new TableauDeBordViewModel
            {
                NombreTotalProduits = await GetNombreTotalProduits(),
                NouveauxClientsInscrits = await GetNouveauxClientsInscrits(),
                ClientsAvecPlusCommandes = await GetClientsAvecPlusCommandes(),
                Top5ProduitsVendus = await GetTop5ProduitsVendus(),
                ChiffreAffairesSemaine = await GetChiffreAffairesSemaine()
            };

            return View(viewModel);
        }

        private async Task<int> GetNombreTotalProduits()
        {
            return await _context.Produit.CountAsync();
        }

        private async Task<int> GetNouveauxClientsInscrits()
        {
            var aujourdhui = DateTime.Today;
            var demain = aujourdhui.AddDays(1);
            return await _context.Users
                .CountAsync(u => u.DateCreation >= aujourdhui && u.DateCreation < demain);
        }

        private async Task<List<ClientCommandeViewModel>> GetClientsAvecPlusCommandes()
        {
            // Récupérer les données brutes
            var clientsData = await _context.Users
                .Where(u => _context.Commande.Any(c => c.ClientId == u.Id))
                .Select(u => new
                {
                    Email = u.Email ?? "N/A",
                    UserId = u.Id
                })
                .ToListAsync();

            // Calculer le nombre de commandes pour chaque client
            var result = new List<ClientCommandeViewModel>();
            foreach (var client in clientsData)
            {
                var nbCommandes = await _context.Commande
                    .CountAsync(c => c.ClientId == client.UserId);

                if (nbCommandes > 0)
                {
                    result.Add(new ClientCommandeViewModel
                    {
                        Email = client.Email,
                        NombreCommandes = nbCommandes
                    });
                }
            }

            return result.OrderByDescending(c => c.NombreCommandes).Take(5).ToList();
        }

        private async Task<List<ProduitVenduViewModel>> GetTop5ProduitsVendus()
        {
            // Récupérer les données de base
            var lignesCommande = await _context.LigneCommande
                .Include(lc => lc.Produit)
                .Select(lc => new
                {
                    ProduitId = lc.ProduitId,
                    NomProduit = lc.Produit.Nom,
                    Quantite = lc.Quantite
                })
                .ToListAsync();

            // Grouper et calculer côté client
            var produitsVendus = lignesCommande
                .GroupBy(lc => new { lc.ProduitId, lc.NomProduit })
                .Select(g => new ProduitVenduViewModel
                {
                    NomProduit = g.Key.NomProduit,
                    QuantiteVendue = g.Sum(x => x.Quantite)
                })
                .OrderByDescending(p => p.QuantiteVendue)
                .Take(5)
                .ToList();

            return produitsVendus;
        }

        private async Task<List<ChiffreAffaireSemaineViewModel>> GetChiffreAffairesSemaine()
        {
            var maintenant = DateTime.Now;
            var debutSemaine = maintenant.AddDays(-7).Date;
            var finSemaine = maintenant.Date.AddDays(1); // Inclure aujourd'hui

            // Récupérer toutes les commandes payées de la semaine avec leurs lignes
            var commandesSemaine = await _context.Commande
                .Where(c => c.DateCreation.Date >= debutSemaine &&
                           c.DateCreation.Date < finSemaine &&
                           c.StatutPaiement == "Payé")
                .Select(c => new
                {
                    Date = c.DateCreation.Date,
                    CommandeId = c.Id,
                    FraisLivraison = c.FraisLivraison
                })
                .ToListAsync();

            // Récupérer les lignes de commande correspondantes
            var commandeIds = commandesSemaine.Select(c => c.CommandeId).ToList();
            var lignesCommande = await _context.LigneCommande
                .Where(lc => commandeIds.Contains(lc.CommandeId))
                .Select(lc => new
                {
                    CommandeId = lc.CommandeId,
                    Total = lc.PrixUnitaire * lc.Quantite
                })
                .ToListAsync();

            // Calculer le total par commande
            var totauxCommandes = lignesCommande
                .GroupBy(lc => lc.CommandeId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total));

            // Calculer le CA par jour
            var ventesParJour = new Dictionary<DateTime, decimal>();
            foreach (var commande in commandesSemaine)
            {
                var totalCommande = totauxCommandes.ContainsKey(commande.CommandeId)
                    ? totauxCommandes[commande.CommandeId] + commande.FraisLivraison
                    : commande.FraisLivraison;

                if (ventesParJour.ContainsKey(commande.Date))
                    ventesParJour[commande.Date] += totalCommande;
                else
                    ventesParJour[commande.Date] = totalCommande;
            }

            // Créer la liste complète des 7 derniers jours
            var joursComplets = new List<ChiffreAffaireSemaineViewModel>();
            for (int i = 6; i >= 0; i--)
            {
                var jour = maintenant.AddDays(-i).Date;
                joursComplets.Add(new ChiffreAffaireSemaineViewModel
                {
                    Date = jour.ToString("dd/MM"),
                    ChiffreAffaires = ventesParJour.ContainsKey(jour) ? ventesParJour[jour] : 0
                });
            }

            return joursComplets;
        }
    }


}