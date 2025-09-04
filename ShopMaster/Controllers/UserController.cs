using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopMaster.Models;

public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private const int PageSize = 10; // Nombre d'utilisateurs par page

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(int? pageIndex, string searchTerm, string roleFilter)
    {
        // Construction de la requête de base
        IQueryable<ApplicationUser> query = _userManager.Users.OrderByDescending(x => x.DateCreation);

        // Application des filtres de recherche
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(u =>
               
                u.Email.ToLower().Contains(searchTerm) ||
                u.UserName.ToLower().Contains(searchTerm)
            );
        }

        // Filtrage par rôle (plus complexe car les rôles sont dans une table séparée)
        if (!string.IsNullOrEmpty(roleFilter))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
            var userIds = usersInRole.Select(u => u.Id).ToList();
            query = query.Where(u => userIds.Contains(u.Id));
        }

        // Validation et initialisation de l'index de page
        if (pageIndex == null || pageIndex < 1)
        {
            pageIndex = 1;
        }

        // Calcul du nombre total d'utilisateurs et de pages
        decimal totalUsers = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalUsers / PageSize);

        // Application de la pagination
        var users = await query
            .Skip(((int)pageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // Passage des données à la vue via ViewBag
        ViewBag.PageIndex = pageIndex;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalUsers = (int)totalUsers;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.RoleFilter = roleFilter;

        return View(users);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Récupération des rôles de l'utilisateur
        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.UserRoles = roles;

        return View(user);
    }

   

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Vérification que ce n'est pas le dernier administrateur
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains("admin"))
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("admin");
            if (adminUsers.Count <= 1)
            {
                TempData["ErrorMessage"] = "Impossible de supprimer le dernier administrateur.";
                return RedirectToAction(nameof(Index));
            }
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Utilisateur supprimé avec succès.";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la suppression de l'utilisateur.";
        }

        return RedirectToAction(nameof(Index));
    }

}