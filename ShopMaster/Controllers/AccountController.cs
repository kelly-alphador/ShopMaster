using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopMaster.Models;
using ShopMaster.Models.DTO;
using ShopMaster.Service.Interface;

namespace ShopMaster.Controllers
{
    public class AccountController : Controller
    {
        //userManagger c'est gestionnaire de l'utilisateur il sert a creer , modifier chercher etc c'est qui concerne l'user
        private readonly UserManager<ApplicationUser> _userManager;
        //gestionnaire de connexion 
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            // Cette action affiche simplement la vue de confirmation
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            //verifie si les donnees sont valide
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            //instanciation de l'objet user et affectation de donnees venant de formulaire
            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.Tel,
                Adress = model.Adress,
                DateCreation = DateTime.Now
            };
            //creation de nouvel utilisateur
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Client");
                _logger.LogInformation("Utilisateur créé avec succès.");

                // Génération du token de confirmation d'email
                var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                //Url.Action permet de generer une url vers une cation au sein de controlleur
                var callbackUrl = Url.Action(
                    "ConfirmEmail",
                    "Account",
                    new { userId = user.Id, token = emailConfirmationToken },
                    Request.Scheme);

                // Envoi de l'email de confirmation
                var emailBody = $@"
                    <html>
                    <body>
                        <h2>Bienvenue sur ShopMaster !</h2>
                        <p>Bonjour {model.Username},</p>
                        <p>Merci de vous être inscrit sur ShopMaster. Pour activer votre compte, veuillez cliquer sur le lien ci-dessous :</p>
                        <p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirmer mon email</a></p>
                        <p>Si le bouton ne fonctionne pas, copiez et collez ce lien dans votre navigateur :</p>
                        <p>{HtmlEncoder.Default.Encode(callbackUrl)}</p>
                        <br>
                        <p>Cordialement,<br>L'équipe ShopMaster</p>
                    </body>
                    </html>";
                //ici il utilise la methode dans l'interface IEmailSender
                await _emailSender.SendEmailAsync(model.Email, "Confirmez votre email - ShopMaster", emailBody);
                //et on envoye les utilisateurs vers register confirmation
                return View("RegisterConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // 3. ACTION POUR EmailConfirmed.cshtml
        [HttpGet]
        public IActionResult EmailConfirmed()
        {
            // Cette action peut être appelée directement ou via ConfirmEmail
            return View();
        }

        // 2. ACTION POUR ConfirmEmail (que vous appelez dans l'email)
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            //ici on verifie si le userId ou token est null et vide 
            //cela verifie si le client a bien cliquer l'url
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return View("Error");
            }
            //Ici on cherche l'user qui a cette id
            var user = await _userManager.FindByIdAsync(userId);
            //si user est null c'est a dire aucun user corrspond a cette id
            if (user == null)
            {
                return View("Error");
            }
            //confirmation de l'email
            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Email confirmé avec succès pour l'utilisateur {user.UserName}");

                // Optionnel : Connecter automatiquement l'utilisateur
                //on met isPersistent a false pour la raison de securite lorsque l'user quitte le navigateur il est deconnecter automatiquement
                await _signInManager.SignInAsync(user, isPersistent: false);

                return View("EmailConfirmed");
            }
            else
            {
                _logger.LogWarning($"Échec de la confirmation d'email pour l'utilisateur {user.UserName}");
                return View("EmailConfirmationError");
            }
        }
        public async Task<IActionResult> Logout()
        {
            if(_signInManager.IsSignedIn(User))
            {
                await _signInManager.SignOutAsync();
            }
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Password()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Password(PasswordDto passwordDto)
        {
            if(!ModelState.IsValid)
            {
                return View(passwordDto);
            }
            var appuser=await _userManager.GetUserAsync(User);
            if (appuser == null) 
            {
                return RedirectToAction("Index", "Homme");
            }
            var result = await _userManager.ChangePasswordAsync(appuser, passwordDto.CurrentPassword, passwordDto.NewPassword);
            if (result.Succeeded)
            {
                ViewBag.SuccessMessage = "Password modifier avec succees!";
            }
            else
            {
                ViewBag.ErrorMessage = "Error: " + result.Errors.First().Description;
            }

            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            //verifier si l'utilisateur est connecter
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }
            //verifie si l'email exist
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Utilisateur non trouvé.";
                return View(loginDto);
            }

            // Vérifiez le mot de passe manuellement
            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid)
            {
                ViewBag.ErrorMessage = "Mot de passe incorrect.";
                return View(loginDto);
            }

            // Connectez directement l'utilisateur
            await _signInManager.SignInAsync(user, loginDto.RememberMe);
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Profile()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = new ProfileDto
            {
                Username = appUser.UserName,
                Email = appUser.Email,
                Tel = appUser.PhoneNumber,
                Adress = appUser.Adress,
            };

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileDto profile)
        {
            // Supprimer les erreurs de validation pour les champs de mot de passe
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            //tester si les utilisateurs a entrer une donnees valide 
            if (!ModelState.IsValid)
            {
                return View(profile);
            }
            //ici on recupere l'utilisateur qu'on va modifier
            var appUser = await _userManager.GetUserAsync(User);
            //on verifie ici s'il est null si il est null c'est a dire l'utilisateur qui n'existe pas on a pas eu l'utilisateur
            if (appUser == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Vérifier si l'email ou le nom d'utilisateur existe déjà
            var existingUserByEmail = await _userManager.FindByEmailAsync(profile.Email);
            if (existingUserByEmail != null && existingUserByEmail.Id != appUser.Id)
            {
                ViewBag.Error = "Cette adresse email est déjà utilisée par un autre utilisateur.";
                return View(profile);
            }
            //ici on verifie si le nom de l'utilisateur n'existe pas
            var existingUserByUsername = await _userManager.FindByNameAsync(profile.Username);
            if (existingUserByUsername != null && existingUserByUsername.Id != appUser.Id)
            {
                ViewBag.Error = "Ce nom d'utilisateur est déjà utilisé par un autre utilisateur.";
                return View(profile);
            }

            // Mettre à jour les informations
            appUser.UserName = profile.Username;
            appUser.Email = profile.Email;
            appUser.Adress = profile.Adress;
            appUser.PhoneNumber = profile.Tel;
            //on persiste ici a la base de donnees

            var result = await _userManager.UpdateAsync(appUser);

            if (result.Succeeded)
            {
                ViewBag.success = "Profil modifié avec succès!";

                // Mettre à jour le modèle avec les nouvelles données
                var updatedProfile = new ProfileDto
                {
                    Username = appUser.UserName,
                    Email = appUser.Email,
                    Tel = appUser.PhoneNumber,
                    Adress = appUser.Adress,
                };

                return View(updatedProfile);
            }
            else
            {
                ViewBag.Error = "Erreur lors de la modification du profil: " + result.Errors.FirstOrDefault()?.Description;
                return View(profile);
            }
        }
    }
}
