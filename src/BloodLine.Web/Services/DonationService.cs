using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Services;

public class DonationService(AppDbContext db)
{
    public async Task Complete(int bankId, CompleteDonationForm f)
    {
        if (!BloodGroups.All.Contains(f.BloodGroup) || !double.IsFinite(f.Units) || f.Units <= 0 || f.Units > 100 || f.ExpiresOn < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException("Invalid blood group, units or expiry date.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var a = await db.Appointments.Include(x => x.Donor).SingleOrDefaultAsync(x => x.Id == f.AppointmentId && x.BloodBankId == bankId);
        if (a is null || a.Status != AppointmentStatus.Open)
            throw new InvalidOperationException("Only an open appointment from your bank can be completed.");
        a.Status = AppointmentStatus.Complete;
        a.Version = Guid.NewGuid();
        a.Donor.BloodGroup = f.BloodGroup;
        db.Donations.Add(new Donation { BloodBankId = bankId, DonorId = a.DonorId, AppointmentId = a.Id, DonatedAt = DateTime.Now, BloodGroup = f.BloodGroup, DonationType = a.DonationType, Units = f.Units, Pulse = f.Pulse, Temperature = f.Temperature, BloodPressure = f.BloodPressure });
        db.Inventory.Add(new InventoryBatch { BloodBankId = bankId, BloodGroup = f.BloodGroup, Units = f.Units, ExpiresOn = f.ExpiresOn });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    public async Task Issue(int bankId, IssueForm f, string userId)
    {
        if (!double.IsFinite(f.Units) || f.Units <= 0 || !BloodGroups.All.Contains(f.BloodGroup))
            throw new InvalidOperationException("Enter a positive quantity and a valid blood group.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var batches = await db.Inventory.Where(x => x.BloodBankId == bankId && x.BloodGroup == f.BloodGroup && x.ExpiresOn >= today && x.Units > 0).OrderBy(x => x.ExpiresOn).ThenBy(x => x.Id).ToListAsync();
        if (batches.Sum(x => x.Units) + 1e-9 < f.Units)
            throw new InvalidOperationException("Not enough unexpired stock is available.");
        var remaining = f.Units;
        foreach (var b in batches)
        {
            var take = Math.Min(b.Units, remaining);
            b.Units -= take;
            b.Version = Guid.NewGuid();
            remaining -= take;
            if (remaining <= 1e-9)
                break;
        }
        db.InventoryIssues.Add(new InventoryIssue { BloodBankId = bankId, BloodGroup = f.BloodGroup, Units = f.Units, Recipient = f.Recipient, IssuedBy = userId });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
