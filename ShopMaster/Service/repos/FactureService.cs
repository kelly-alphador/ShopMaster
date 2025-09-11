using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShopMaster.Context;
using ShopMaster.Models;
using ShopMaster.Service.Interface;

namespace ShopMaster.Service.repos
{
    public class FactureService:IFactureService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender; // Utilisation de votre service existant
        private readonly ILogger<FactureService> _logger;

        public FactureService(ApplicationDbContext context, IEmailSender emailSender, ILogger<FactureService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<FactureViewModel> GenererFactureAsync(int commandeId)
        {
            try
            {
                // Traduction de votre première requête SQL en LINQ
                var itemsFacture = await _context.LigneCommande
                    .Where(lc => lc.CommandeId == commandeId)
                    .Join(_context.Produit,
                        lc => lc.ProduitId,
                        p => p.Id,
                        (lc, p) => new FactureItem
                        {
                            NomProduit = p.Nom,
                            Quantite = lc.Quantite,
                            PrixUnitaire = lc.PrixUnitaire,
                            PrixTotal = lc.Quantite * lc.PrixUnitaire
                        })
                    .ToListAsync();

                // Traduction de votre deuxième requête SQL en LINQ
                var prixTotal = await _context.LigneCommande
                    .Where(lc => lc.CommandeId == commandeId)
                    .SumAsync(lc => lc.Quantite * lc.PrixUnitaire);

                // Récupérer les informations de la commande
                var commande = await _context.Commande
                    .FirstOrDefaultAsync(c => c.Id == commandeId);

                var facture = new FactureViewModel
                {
                    CommandeId = commandeId,
                    Items = itemsFacture,
                    PrixTotalCommande = prixTotal,
                    DateCommande = commande?.DateCreation ?? DateTime.Now
                };

                return facture;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la génération de la facture pour la commande {commandeId}");
                throw;
            }
        }

        public async Task EnvoyerFactureParEmailAsync(int commandeId, string emailClient)
        {
            try
            {
                var facture = await GenererFactureAsync(commandeId);
                facture.EmailClient = emailClient;

                var htmlFacture = GenererHtmlFacture(facture);

                // Utilisation de votre IEmailSender existant
                await _emailSender.SendEmailAsync(
                    emailClient,
                    $"Facture de votre commande {facture.NumeroCommande} - ShopMaster",
                    htmlFacture
                );

                _logger.LogInformation($"Facture envoyée avec succès pour la commande {commandeId} à {emailClient}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de la facture pour la commande {commandeId} à {emailClient}");
                throw;
            }
        }

        private string GenererHtmlFacture(FactureViewModel facture)
        {
            var html = $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Facture {facture.NumeroCommande}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: 'Arial', sans-serif; 
            line-height: 1.6; 
            color: #333; 
            background-color: #f4f4f4; 
            padding: 20px;
        }}
        .container {{ 
            max-width: 800px; 
            margin: 0 auto; 
            background-color: white; 
            border-radius: 8px; 
            box-shadow: 0 0 20px rgba(0,0,0,0.1); 
            overflow: hidden;
        }}
        .header {{ 
            background: #1565C0;
            color: white; 
            padding: 30px; 
            text-align: center; 
        }}
        .header h1 {{ 
            font-size: 2.5em; 
            margin-bottom: 10px; 
            font-weight: bold;
        }}
        .header h2 {{ 
            font-size: 1.2em; 
            opacity: 0.9; 
        }}
        .content {{ padding: 30px; }}
        .info-section {{ 
            display: flex; 
            justify-content: space-between; 
            margin-bottom: 30px; 
            padding: 20px; 
            background-color: #f8f9fa; 
            border-radius: 6px;
        }}
        .info-block h3 {{ 
            color: #667eea; 
            margin-bottom: 8px; 
            font-size: 1.1em;
        }}
        .info-block p {{ 
            margin: 4px 0; 
            color: #555;
        }}
        .table-container {{ 
            margin: 30px 0; 
            border-radius: 8px; 
            overflow: hidden; 
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        table {{ 
            width: 100%; 
            border-collapse: collapse; 
        }}
        th {{ 
            background: #1565C0;
            color: white; 
            padding: 15px; 
            text-align: left; 
            font-weight: bold;
        }}
        td {{ 
            padding: 15px; 
            border-bottom: 1px solid #eee; 
        }}
        tr:hover {{ background-color: #f8f9fa; }}
        .text-right {{ text-align: right; }}
        .total-section {{ 
            background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
            padding: 25px; 
            border-radius: 8px; 
            margin-top: 30px;
        }}
        .total-amount {{ 
            font-size: 1.5em; 
            font-weight: bold; 
            color: #667eea; 
            text-align: right;
        }}
        .footer {{ 
            text-align: center; 
            margin-top: 30px; 
            padding: 20px; 
            border-top: 2px solid #eee; 
            color: #777;
        }}
        .footer p {{ margin: 5px 0; }}
        .highlight {{ color: #667eea; font-weight: bold; }}
        @media (max-width: 600px) {{
            .info-section {{ flex-direction: column; }}
            .info-block {{ margin-bottom: 15px; }}
            th, td {{ padding: 10px 8px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ShopMaster</h1>
            <h2>Facture N° {facture.NumeroCommande}</h2>
        </div>
        
        <div class='content'>
            <div class='info-section'>
                <div class='info-block'>
                    <h3>Informations de commande</h3>
                    <p><strong>Numéro:</strong> <span class='highlight'>{facture.NumeroCommande}</span></p>
                    <p><strong>Date:</strong> {facture.DateCommande:dd/MM/yyyy à HH:mm}</p>
                    <p><strong>Statut:</strong> <span style='color: #28a745; font-weight: bold;'>Payée</span></p>
                </div>
                <div class='info-block'>
                    <h3> Client</h3>
                    <p><strong>Email:</strong> {facture.EmailClient}</p>
                </div>
            </div>
            
            <div class='table-container'>
                <table>
                    <thead>
                        <tr>
                            <th> Produit</th>
                            <th class='text-right'>Qté</th>
                            <th class='text-right'>Prix unitaire</th>
                            <th class='text-right'>Total</th>
                        </tr>
                    </thead>
                    <tbody>";

            foreach (var item in facture.Items)
            {
                var euro = CultureInfo.GetCultureInfo("fr-FR");

                html += $@"
                <tr>
                    <td><strong>{item.NomProduit}</strong></td>
                    <td class='text-right'>{item.Quantite}</td>
                    <td class=""text-right"">{item.PrixUnitaire.ToString("C", euro)}</td>
                    <td class=""text-right""><strong>{item.PrixTotal.ToString("C", euro)}</strong></td>
                </tr>";
            }

            html += $@"
                    </tbody>
                </table>
            </div>
            
            <div class='total-section'>
                <div class='total-amount'>
                     Total de la commande: @{facture.PrixTotalCommande.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))}
                </div>
            </div>

            
            <div class='footer'>
                <p><strong>Merci pour votre confiance ! </strong></p>
                <p>Cette facture confirme le paiement de votre commande.</p>
                <p>Pour toute question, n'hésitez pas à nous contacter.</p>
                <p style='margin-top: 15px; font-size: 12px; color: #999;'>
                    ShopMaster - Votre boutique en ligne de confiance
                </p>
            </div>
        </div>
    </div>
</body>
</html>";

            return html;
        }
    }
}
