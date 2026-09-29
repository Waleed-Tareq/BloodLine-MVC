using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Manager)]
public class BankController(AppDbContext db, BankScope scope) : Controller
{
    [HttpGet] public async Task<IActionResult> Edit() => View(await db.BloodBanks.FindAsync(await scope.GetBankId(User)));
    [HttpPost]
    public async Task<IActionResult> Edit([Bind("Name,Email,Phone,Latitude,Longitude,OpensAt,ClosesAt")] BloodBank f)
    {
        if (f.OpensAt >= f.ClosesAt)
            ModelState.AddModelError("", "Closing time must be after opening time.");
        if (!ModelState.IsValid)
            return View(f);
        var bank = (await db.BloodBanks.FindAsync(await scope.GetBankId(User)))!;
        bank.Name = f.Name;
        bank.Email = f.Email;
        bank.Phone = f.Phone;
        bank.Latitude = f.Latitude;
        bank.Longitude = f.Longitude;
        bank.OpensAt = f.OpensAt;
        bank.ClosesAt = f.ClosesAt;
        await db.SaveChangesAsync();
        TempData["Success"] = "Contact details updated.";
        return RedirectToAction(nameof(Edit));
    }
}
