using BloodLine.Web.Data;
using BloodLine.Web.Services;
using BloodLine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize]
public class HomeController(AppDbContext db, BankScope scope) : Controller
{
    public async Task<IActionResult> Index()
    {
        var m = new DashboardViewModel();
        if (User.IsInRole("Admin"))
        {
            m.PendingRequests = await db.RegistrationRequests.CountAsync(x => x.Status == "Pending");
            m.Staff = await db.Users.CountAsync();
            m.Donors = await db.BloodBanks.CountAsync();
        }
        else
        {
            var bank = await scope.GetBankId(User);
            m.BankName = (await db.BloodBanks.FindAsync(bank))!.Name;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var date = DateOnly.FromDateTime(today);
            m.Donors = await db.Donors.CountAsync(x => x.BloodBankId == bank);
            m.Appointments = await db.Appointments.CountAsync(x => x.BloodBankId == bank && x.ScheduledAt >= today && x.ScheduledAt < tomorrow);
            m.AvailableUnits = await db.Inventory.Where(x => x.BloodBankId == bank && x.ExpiresOn >= date).SumAsync(x => x.Units);
            m.Staff = await db.Users.CountAsync(x => x.BloodBankId == bank);
            m.Events = await db.Events.CountAsync(x => x.BloodBankId == bank && x.StartsAt >= today);
        }
        return View(m);
    }
    [AllowAnonymous, IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View();
    }
}
