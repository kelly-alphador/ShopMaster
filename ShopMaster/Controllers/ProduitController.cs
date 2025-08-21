using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using ShopMaster.Context;
using ShopMaster.Models;
using ShopMaster.Models.DTO;

namespace ShopMaster.Controllers
{
    public class ProduitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProduitController(ApplicationDbContext context,IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _context = context;
        }
        public IActionResult Index()
        {
            var produits=_context.Produit.OrderByDescending(p=>p.Id).ToList();
            return View(produits);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(ProduitDto produitDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(produitDto);
                }
                var produitExist = await _context.Produit.FirstOrDefaultAsync(p => p.Nom.ToLower() == produitDto.Nom.ToLower() && p.Marque.ToLower() == produitDto.Marque.ToLower());
                if (produitExist != null)
                {
                    ModelState.AddModelError("", "cette produit existe deja");
                    return View(produitDto);
                }
                string cheminUrl = null;
                try
                {
                    var ExtensionAutoriser = new[] { ".jpg", ".jpeg", ".png" };
                    var Extension = Path.GetExtension(produitDto.ImageUrl.FileName).ToLowerInvariant();
                    if (!ExtensionAutoriser.Contains(Extension))
                    {
                        ModelState.AddModelError(nameof(produitDto.ImageUrl), "seul le .jpg .jpeg .png peut etre enregistrer");
                        return View(produitDto);
                    }
                    if (produitDto.ImageUrl.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(nameof(produitDto.ImageUrl), "la taille depasse de la taille max");
                        return View(produitDto);
                    }
                    var NomImageUnique = $"{Guid.NewGuid()}{Extension}";
                    var dossier = Path.Combine(_webHostEnvironment.WebRootPath, "product");
                    if (!Directory.Exists(dossier))
                    {
                        Directory.CreateDirectory(dossier);
                    }
                    var CheminComplet = Path.Combine(dossier, NomImageUnique);
                    using (var stream = new FileStream(CheminComplet, FileMode.Create))
                    {
                        await produitDto.ImageUrl.CopyToAsync(stream);
                    }
                    cheminUrl = NomImageUnique;
                }
                catch (IOException ex)
                {
                    ModelState.AddModelError(string.Empty, "Erreur lors de la sauvegarde de l'image. veuillez ressayer");
                    return View(produitDto);
                }

                // 4. Création de l'entité Produit
                var nouveauProduit = new Produit
                {
                    Nom = produitDto.Nom.Trim(),
                    Marque = produitDto.Marque.Trim(),
                    qte = produitDto.qte,
                    Prix = produitDto.Prix,
                    Description = produitDto.Description.Trim(),
                    ImageUrl = cheminUrl,
                    DateCreation = DateTime.Now,
                };
                try
                {
                    // 5. Ajout en base de données
                    _context.Produit.Add(nouveauProduit);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Le produit '{nouveauProduit.Nom}' a été ajouté avec succès.";
                    // 7. Redirection
                    return RedirectToAction("Index", "Produit");
                }
                catch (DbUpdateException ex)
                {
                    var CheminImage = Path.Combine(_webHostEnvironment.WebRootPath, "product", cheminUrl);
                    if (System.IO.File.Exists(CheminImage))
                    {
                        System.IO.File.Delete(CheminImage);
                    }
                    ModelState.AddModelError(string.Empty, "Erreur lors de l'enregistrement en base de données. Veuillez réessayer.");
                    return View(produitDto);
                }
            }
            catch (Exception Ex)
            {
                ModelState.AddModelError(string.Empty, "une erreur s'est produit lors de l'ajout");
                return View(produitDto);
            }
        }
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var produitExist = await _context.Produit.FindAsync(id);
                if (produitExist == null)
                {
                    TempData["ErrorMessage"] = "produit introuvable";
                    return RedirectToAction("Index", "Produit");
                }
                var produitDto = new ProduitDto
                {
                    Nom = produitExist.Nom,
                    Marque = produitExist.Marque,
                    Description = produitExist.Description,
                    Prix = produitExist.Prix,
                    qte = produitExist.qte,
                };
                ViewData["ProductId"] = produitExist.Id;
                ViewData["ImageFileName"] = produitExist.ImageUrl;
                ViewData["CreatedAt"] = produitExist.DateCreation;
                return View(produitDto);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Une erreur s'est produite lors du chargement du produit.";
                return RedirectToAction("Index", "Products");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProduitDto produitdto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var produit = await _context.Produit.FindAsync(id);
                    if (produit != null) 
                    {
                        ViewData["ProductId"] = produit.Id;
                        ViewData["ImageFileName"] = produit.ImageUrl;
                        ViewData["CreatedAt"] = produit.DateCreation.ToString("dd/MM/yyyy");
                    }
                    return View(produitdto);
                }

                var produitExist = await _context.Produit.FindAsync(id);
                if (produitExist == null)
                {
                    TempData["error"] = "cette id n'existe pas";
                    return RedirectToAction("Index", "Produit");
                }

                var produitExistNom = await _context.Produit.FirstOrDefaultAsync(p => p.Nom.ToLower() == produitdto.Nom.ToLower() && p.Marque.ToLower() == produitdto.Marque.ToLower() && p.Id != id);
                if (produitExistNom != null)
                {
                    ModelState.AddModelError(string.Empty, "Un produit avec ce nom et cette marque existe déjà.");
                    ViewData["ProductId"] = produitExist.Id;
                    ViewData["ImageFileName"] = produitExist.ImageUrl;
                    ViewData["CreatedAt"] = produitExist.DateCreation.ToString("dd/MM/yyyy"); 
                    return View(produitdto);
                }

                string ImageFile = produitExist.ImageUrl;
                if (produitdto.ImageUrl != null)
                {
                    var extensionAuthorizer = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(produitdto.ImageUrl.FileName).ToLowerInvariant();
                    if (!extensionAuthorizer.Contains(extension))
                    {
                        ModelState.AddModelError(nameof(produitdto.ImageUrl), "seul .jpg et .jpeg .png");
                        ViewData["ProductId"] = produitExist.Id;
                        ViewData["ImageFileName"] = produitExist.ImageUrl;
                        ViewData["CreatedAt"] = produitExist.DateCreation.ToString("dd/MM/yyyy"); 
                        return View(produitdto);
                    }
                    if (produitdto.ImageUrl.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(nameof(produitdto.ImageUrl), "la taille de cette image depasse 5MB ");
                        ViewData["ProductId"] = produitExist.Id;
                        ViewData["ImageFileName"] = produitExist.ImageUrl;
                        ViewData["CreatedAt"] = produitExist.DateCreation.ToString("dd/MM/yyyy"); 
                        return View(produitdto);
                    }
                    try
                    {
                        var nomUnique = $"{Guid.NewGuid()}{extension}";
                        var dossierImage = Path.Combine(_webHostEnvironment.WebRootPath, "product");
                        if (!Directory.Exists(dossierImage))
                        {
                            Directory.CreateDirectory(dossierImage);
                        }
                        var cheminComplet = Path.Combine(dossierImage, nomUnique);
                        using (var stream = new FileStream(cheminComplet, FileMode.Create))
                        {
                            await produitdto.ImageUrl.CopyToAsync(stream);
                        }
                        var imageAncien = Path.Combine(dossierImage, produitExist.ImageUrl);
                        if (System.IO.File.Exists(imageAncien))
                        {
                            System.IO.File.Delete(imageAncien);
                        }
                        ImageFile = nomUnique;
                    }
                    catch (IOException)
                    {
                        ModelState.AddModelError(string.Empty, "Erreur lors de la sauvegarde de l'image. Veuillez réessayer.");
                        ViewData["ProductId"] = produitExist.Id;
                        ViewData["ImageFileName"] = produitExist.ImageUrl;
                        ViewData["CreatedAt"] = produitExist.DateCreation.ToString("dd/MM/yyyy");
                        return View(produitdto);
                    }
                } 

                
                produitExist.Nom = produitdto.Nom.Trim();
                produitExist.Marque = produitdto.Marque.Trim();
                produitExist.qte = produitdto.qte; 
                produitExist.Prix = produitdto.Prix;
                produitExist.Description = produitdto.Description?.Trim();
                produitExist.ImageUrl = ImageFile;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["success"] = $"le produit {produitdto.Nom} a été modifié avec succès";
                    return RedirectToAction("Index", "Produit");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Erreur lors de la mise à jour en base de données. Veuillez réessayer.");
                    ViewData["ProductId"] = produitExist.Id;
                    ViewData["ImageFileName"] = produitExist.ImageUrl;
                    ViewData["CreatedAt"] = produitExist.DateCreation.ToString("dd/MM/yyyy");
                    return View(produitdto);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "une erreur se produit";
                return RedirectToAction("Index", "Produit");
            }
        
        }
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var produitExist = await _context.Produit.FindAsync(id);
                if (produitExist == null)
                {
                    TempData["error"] = "cette produit n'existe pas";
                    return RedirectToAction("Index", "Produit");
                }
                var ImagePathUrl = _webHostEnvironment.WebRootPath + "product" + produitExist.ImageUrl;
                System.IO.File.Delete(ImagePathUrl);
                _context.Produit.Remove(produitExist);
                _context.SaveChanges(true);
                TempData["SuccessMessage"] = "donnees supprimer avec succees";
                return RedirectToAction("Index", "Produit");
            }
            catch (Exception ex) 
            {
                TempData["error"] = "Error lors de la suppression";
                return RedirectToAction("Index", "Produit");
            }

        }
    }
}
