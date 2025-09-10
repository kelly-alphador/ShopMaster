using System.Text.Json;
using ShopMaster.Context;
using ShopMaster.Models;

namespace ShopMaster.Service.repos
{
    public class CartHelper
    {
      public static Dictionary<int, int> GetCartDictionary(HttpRequest request, HttpResponse response)
        {
            // On récupère les données dans le cookie si il est vide on le met à null
            string cookieValue = request.Cookies["shopping_cart"] ?? "";

            Console.WriteLine($"[CartHelper] Raw cookie value: '{cookieValue}'");
            Console.WriteLine($"[CartHelper] Cookie length: {cookieValue.Length}");

            try
            {
                var cart = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cookieValue));
                Console.WriteLine($"[CartHelper] Decoded cart: '{cart}'");

                var dictionary = JsonSerializer.Deserialize<Dictionary<int, int>>(cart);
                Console.WriteLine($"[CartHelper] Deserialized dictionary count: {dictionary?.Count ?? 0}");

                if (dictionary != null)
                {
                    return dictionary;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CartHelper] Exception: {ex.Message}");
            }

            if (cookieValue.Length > 0)
            {
                Console.WriteLine("[CartHelper] Deleting invalid cookie");
                // Si le cookie n'est pas valide on le supprime
                response.Cookies.Delete("shopping_cart");
            }

            Console.WriteLine("[CartHelper] Returning empty dictionary");
            return new Dictionary<int, int>();
        }
        public static List<LigneCommande> GetCartItems(HttpRequest request, HttpResponse response, ApplicationDbContext context)
        {
            var dict = GetCartDictionary(request, response);
            var cartItems = new List<LigneCommande>();

            foreach (var kvp in dict) // kvp.Key = ProduitId, kvp.Value = Quantité
            {
                var produit = context.Produit.FirstOrDefault(p => p.Id == kvp.Key);
                if (produit != null)
                {
                    cartItems.Add(new LigneCommande
                    {
                        ProduitId = produit.Id,       // ✅ ici on met bien l'ID du produit
                        Produit = produit,
                        Quantite = kvp.Value,
                        PrixUnitaire = produit.Prix
                    });
                }
            }

            return cartItems;
        }

        public static int GetCartSize(HttpRequest request, HttpResponse response)
        {
            int cartSize = 0;
            var cartDictionary = GetCartDictionary(request, response);
            foreach (var keyValuePair in cartDictionary)
            {
                //a chaque iteration on ajoute la quantite de produit cartsize=cartsize+quantite de chaque produit
                cartSize += keyValuePair.Value;
            }
            //retourne nombre total de produit dans le pannier
            return cartSize;
        }

       /* public static List<LigneCommande> GetCartItems(HttpRequest request, HttpResponse response, ApplicationDbContext context)
        {
            var cartItems = new List<LigneCommande>();
            var cartDictionary = GetCartDictionary(request, response);
            //le cartDictionary contient les donnes dans le cookie cle et valeur {"1": 5, "6": 2} 1 idproduit 5 valeur
            foreach (var item in cartDictionary)
            {
                int productId = item.Key;
                int quantity = item.Value;

                var product = context.Produit.Find(productId);
                if (product != null)
                {
                    var orderItem = new LigneCommande()
                    {
                        Produit = product,
                        Quantite = quantity,
                        PrixUnitaire = product.Prix
                    };
                    //on ajoute les donnees dans l'objet cartItems
                    cartItems.Add(orderItem);
                }
            }

            return cartItems;
        }*/
        //cette methode sert a retourner le prix total de produit dans pannier
        public static decimal GetSubtotal(List<LigneCommande> cartItems)
        {
            decimal subtotal = 0;
            foreach (var item in cartItems)
            {
                subtotal += item.Quantite * item.PrixUnitaire;
            }
            return subtotal;
        }

        public static void SaveCartDictionary(Dictionary<int, int> cart, HttpRequest request, HttpResponse response)
        {
            string json = JsonSerializer.Serialize(cart);
            string cookieValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            var cookieOptions = new CookieOptions()
            {
                MaxAge = TimeSpan.FromDays(365),
                Path = "/",
                SameSite = SameSiteMode.Strict,
                Secure = true // Mettre à false en développement si pas de HTTPS
            };

            response.Cookies.Append("shopping_cart", cookieValue, cookieOptions);
        }

        public static void ClearCart(HttpResponse response)
        {
            response.Cookies.Delete("shopping_cart");
        }
    }
}
