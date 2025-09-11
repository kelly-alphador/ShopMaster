using PayPal.Api;
using ShopMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PayPal;
using ShopMaster.Models.DTO;

namespace ShopMaster.Service.repos
{
    public class PayPalService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(IConfiguration configuration, ILogger<PayPalService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public APIContext GetAPIContext()
        {
            try
            {
                var config = new Dictionary<string, string>
                {
                    {"mode", _configuration["PayPal:Mode"]},
                    {"clientId", _configuration["PayPal:ClientId"]},
                    {"clientSecret", _configuration["PayPal:ClientSecret"]}
                };

                var accessToken = new OAuthTokenCredential(config).GetAccessToken();
                var apiContext = new APIContext(accessToken)
                {
                    Config = config
                };

                return apiContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du contexte API PayPal");
                throw;
            }
        }

        public Payment CreatePayment(decimal total, string currency, string returnUrl, string cancelUrl, List<PayPalItemDTO> items)
        {
            try
            {
                var apiContext = GetAPIContext();

                // Vérification des montants
                decimal subtotal = items.Sum(item => item.Price * item.Quantity);
                decimal shipping = 5.99m;

                if (Math.Abs(total - (subtotal + shipping)) > 0.01m)
                {
                    throw new Exception($"Incohérence dans les montants: total={total}, subtotal+shipping={subtotal + shipping}");
                }

                var payment = new Payment
                {
                    intent = "sale",
                    payer = new Payer { payment_method = "paypal" },
                    transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            description = "Commande ShopMaster",
                            invoice_number = Guid.NewGuid().ToString(),
                            amount = new Amount
                            {
                                currency = currency,
                                total = total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                details = new Details
                                {
                                    subtotal = subtotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                    shipping = shipping.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                }
                            },
                            item_list = new ItemList
                            {
                                items = items.Select(item => new Item
                                {
                                    name = item.Name.Substring(0, Math.Min(item.Name.Length, 127)),
                                    currency = currency,
                                    price = item.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                    quantity = item.Quantity.ToString(),
                                    sku = item.Sku.Substring(0, Math.Min(item.Sku.Length, 50))
                                }).ToList()
                            }
                        }
                    },
                    redirect_urls = new RedirectUrls
                    {
                        cancel_url = cancelUrl,
                        return_url = returnUrl
                    }
                };

                var createdPayment = payment.Create(apiContext);
                _logger.LogInformation($"Paiement PayPal créé avec succès: {createdPayment.id}");
                return createdPayment;
            }
            catch (PayPalException payPalEx)
            {
                
                _logger.LogError(payPalEx, $"Erreur PayPal: {payPalEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du paiement PayPal");
                throw;
            }
        }

        public Payment ExecutePayment(string paymentId, string payerId)
        {
            try
            {
                var apiContext = GetAPIContext();
                var paymentExecution = new PaymentExecution { payer_id = payerId };
                var payment = new Payment { id = paymentId };

                var executedPayment = payment.Execute(apiContext, paymentExecution);
                _logger.LogInformation($"Paiement PayPal exécuté avec succès: {executedPayment.id}");
                return executedPayment;
            }
            catch (PayPalException payPalEx)
            {
             
                _logger.LogError(payPalEx, $"Erreur PayPal lors de l'exécution: {payPalEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution du paiement PayPal");
                throw;
            }
        }
    }
}