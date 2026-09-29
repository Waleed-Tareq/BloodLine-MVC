using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using BloodLine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Staff)]
public class InventoryController(AppDbContext db, BankScope scope, DonationService service) : Controller
{
    public async Task<IActionResult> Index()
    {
        var bank = await scope.GetBankId(User);
        ViewBag.Issues = await db.InventoryIssues.Where(x => x.BloodBankId == bank).OrderByDescending(x => x.IssuedAt).Take(20).ToListAsync();
        return View(await db.Inventory.Where(x => x.BloodBankId == bank).OrderBy(x => x.ExpiresOn).ToListAsync());
    }
    [HttpGet] public IActionResult Issue() => View(new IssueForm());
    [HttpPost]
    public async Task<IActionResult> Issue(IssueForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        try
        {
            await service.Issue(await scope.GetBankId(User), f, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            TempData["Success"] = "Stock issued, using the earliest expiry first.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException e) { ModelState.AddModelError("", e.Message); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Stock changed. Refresh and try again."); }
        return View(f);
    }
}
