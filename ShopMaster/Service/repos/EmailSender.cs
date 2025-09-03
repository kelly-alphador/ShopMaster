using ShopMaster.Service.Interface;

namespace ShopMaster.Service.repos
{
    public class EmailSender : IEmailSender
    {
        //on inject Iconfiguration cela peremettre a la class EmailSender d'acceder a appsettings.json pour savoir l'information sur EmailSettings 
        private readonly IConfiguration _configuration;
        //c'est pour l'enregistrement des evenements
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                //instanciation de smptCLient 
                //le serveur SMTP est responsable de l'envoye de l'email
                //on utilise using pour liberer la memoir apres l'utilisation
                using var client = new System.Net.Mail.SmtpClient();

                // Configuration SMTP
                //_configuration[] il enleve la configuration dans appsettings.json
                //ici il enleve l'adresse de server smtp si il n'est trouve pas il prend le valeur par defaut
                client.Host = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                //ici le port
                client.Port = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                //ici il utilise le protocole de securisation
                client.EnableSsl = true;
                //on met UseDefaultCredentials en false car s'il est entrue il utilise les informations de l'utilisateurs actuel connecte utilisateur windows
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]
                );
                //Ici c'est l'object qui contient tous les information sur message
                var mailMessage = new System.Net.Mail.MailMessage
                {
                    //ici pour configurer qui a envoyer le message
                    From = new System.Net.Mail.MailAddress(
                        _configuration["EmailSettings:Username"] ?? "shopmaster@gmail.com",
                        "ShopMaster"
                    ),
                    //ici le sujet
                    Subject = subject,
                    //message
                    Body = htmlMessage,
                    //on met cela a true pour montrer que le message est format HTML
                    IsBodyHtml = true
                };
                //ici c'est l'adresse de destinataire
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email envoyé avec succès à {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de l'email à {email}");
                throw;
            }
        }
    }
}
