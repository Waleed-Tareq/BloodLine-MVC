using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Staff)]
public class NeedsController(AppDbContext db, BankScope scope) : Controller
{
    public async Task<IActionResult> Index()
    {
        var bank = await scope.GetBankId(User);
        return View(await db.BloodNeeds.Where(x => x.BloodBankId == bank).OrderByDescending(x => x.ExpiresAt).ToListAsync());
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return View(new BloodNeed());
        var bank = await scope.GetBankId(User);
        var f = await db.BloodNeeds.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        return f == null ? NotFound() : View(f);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int? id, [Bind("BloodGroup,Units,Location,Hospital,ExpiresAt")] BloodNeed f)
    {
        var bank = await scope.GetBankId(User);
        if (f.ExpiresAt < DateTime.Now)
            ModelState.AddModelError(nameof(f.ExpiresAt), "Choose a future date and time.");
        if (!ModelState.IsValid)
            return View(f);
        if (id == null)
        {
            f.BloodBankId = bank;
            db.BloodNeeds.Add(f);
        }
        else
        {
            var saved = await db.BloodNeeds.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
            if (saved == null)
                return NotFound();
            saved.BloodGroup = f.BloodGroup;
            saved.Units = f.Units;
            saved.Location = f.Location;
            saved.Hospital = f.Hospital;
            saved.ExpiresAt = f.ExpiresAt;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Saved successfully.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var bank = await scope.GetBankId(User);
        var f = await db.BloodNeeds.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        if (f == null)
            return NotFound();
        db.BloodNeeds.Remove(f);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
