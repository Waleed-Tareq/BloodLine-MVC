using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Staff)]
public class DonorsController(AppDbContext db, BankScope scope) : Controller
{
    public async Task<IActionResult> Index(string? q, string? bloodGroup, bool volunteers = false)
    {
        var bank = await scope.GetBankId(User);
        var list = db.Donors.Where(x => x.BloodBankId == bank);
        if (!string.IsNullOrWhiteSpace(q))
            list = list.Where(x => x.Name.Contains(q) || x.Email.Contains(q));
        if (!string.IsNullOrWhiteSpace(bloodGroup))
            list = list.Where(x => x.BloodGroup == bloodGroup);
        if (volunteers)
            list = list.Where(x => x.IsVolunteer);
        ViewBag.Volunteers = volunteers;
        return View(await list.OrderBy(x => x.Name).ToListAsync());
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return View(new Donor());
        var bank = await scope.GetBankId(User);
        var d = await db.Donors.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        return d == null ? NotFound() : View(d);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int? id, [Bind("Name,Email,Phone,BloodGroup,DateOfBirth,Gender,Weight,IdNumber,Conditions,IsVolunteer")] Donor f)
    {
        var bank = await scope.GetBankId(User);
        if (f.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(f.DateOfBirth), "Date of birth cannot be in the future.");
        if (await db.Donors.AnyAsync(x => x.BloodBankId == bank && x.Email == f.Email && x.Id != id))
            ModelState.AddModelError(nameof(f.Email), "A donor with this email already exists in your bank.");
        if (!ModelState.IsValid)
            return View(f);
        if (id == null)
        {
            f.BloodBankId = bank;
            db.Donors.Add(f);
        }
        else
        {
            var d = await db.Donors.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
            if (d == null)
                return NotFound();
            d.Name = f.Name;
            d.Email = f.Email;
            d.Phone = f.Phone;
            d.BloodGroup = f.BloodGroup;
            d.DateOfBirth = f.DateOfBirth;
            d.Gender = f.Gender;
            d.Weight = f.Weight;
            d.IdNumber = f.IdNumber;
            d.Conditions = f.Conditions;
            d.IsVolunteer = f.IsVolunteer;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Donor saved.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> History(int id)
    {
        var bank = await scope.GetBankId(User);
        var donor = await db.Donors.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        if (donor == null)
            return NotFound();
        ViewBag.DonorName = donor.Name;
        return View(await db.Donations.Where(x => x.DonorId == id && x.BloodBankId == bank).OrderByDescending(x => x.DonatedAt).ToListAsync());
    }
}
