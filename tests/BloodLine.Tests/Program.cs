using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using BloodLine.Web.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var passed = 0;
async Task Test(string name, Func<Task> run) { await run(); passed++; Console.WriteLine($"PASS {name}"); }
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
async Task Reject(Func<Task> action) { try { await action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected rejection."); }
async Task<(AppDbContext db, SqliteConnection connection)> Database()
{
    var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    await db.Database.EnsureCreatedAsync();
    var donor = new Donor { Id = 1, BloodBankId = 1, Name = "Test", Email = "test@example.test", BloodGroup = "O+", Weight = 65 };
    db.Donors.Add(donor);
    db.Appointments.Add(new Appointment { Id = 1, BloodBankId = 1, DonorId = 1, ScheduledAt = DateTime.Today, Status = AppointmentStatus.Open });
    await db.SaveChangesAsync();
    return (db, connection);
}
CompleteDonationForm Form() => new() { AppointmentId = 1, BloodGroup = "O+", Units = 1, Pulse = 72, Temperature = 36.5, BloodPressure = "120/80", ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(42)) };
await Test("Completing an appointment records one donation and one inventory batch", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    await new DonationService(db).Complete(1, Form());
    Check(await db.Donations.CountAsync() == 1, "Donation missing");
    Check(await db.Inventory.SumAsync(x => x.Units) == 1, "Stock missing");
    Check((await db.Appointments.FindAsync(1))!.Status == AppointmentStatus.Complete, "Status not complete");
});
await Test("Completing twice cannot increase stock twice", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    var service = new DonationService(db);
    await service.Complete(1, Form());
    await Reject(() => service.Complete(1, Form()));
    Check(await db.Donations.CountAsync() == 1, "Duplicate donation");
    Check(await db.Inventory.SumAsync(x => x.Units) == 1, "Duplicate stock");
});
await Test("A different bank cannot complete the appointment", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    await Reject(() => new DonationService(db).Complete(2, Form()));
    Check(await db.Donations.CountAsync() == 0, "Cross-bank donation");
});
await Test("A pending appointment cannot be completed", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    (await db.Appointments.FindAsync(1))!.Status = AppointmentStatus.Pending;
    await db.SaveChangesAsync();
    await Reject(() => new DonationService(db).Complete(1, Form()));
});
await Test("Stock issues consume earliest expiry and exclude expired or other-bank stock", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    var today = DateOnly.FromDateTime(DateTime.Today);
    db.Inventory.AddRange(new InventoryBatch { Id = 1, BloodBankId = 1, BloodGroup = "O+", Units = 8, ExpiresOn = today.AddDays(-1) }, new InventoryBatch { Id = 2, BloodBankId = 1, BloodGroup = "O+", Units = 2, ExpiresOn = today.AddDays(1) }, new InventoryBatch { Id = 3, BloodBankId = 1, BloodGroup = "O+", Units = 5, ExpiresOn = today.AddDays(3) }, new InventoryBatch { Id = 4, BloodBankId = 2, BloodGroup = "O+", Units = 10, ExpiresOn = today });
    await db.SaveChangesAsync();
    await new DonationService(db).Issue(1, new IssueForm { BloodGroup = "O+", Units = 3, Recipient = "Test hospital" }, "test");
    Check((await db.Inventory.FindAsync(1))!.Units == 8, "Expired batch used");
    Check((await db.Inventory.FindAsync(2))!.Units == 0, "Earliest batch not used first");
    Check((await db.Inventory.FindAsync(3))!.Units == 4, "Wrong remaining units");
    Check((await db.Inventory.FindAsync(4))!.Units == 10, "Other bank modified");
    Check(await db.InventoryIssues.CountAsync() == 1, "Issue audit missing");
});
await Test("Insufficient stock leaves inventory and audit unchanged", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    db.Inventory.Add(new InventoryBatch { BloodBankId = 1, BloodGroup = "A+", Units = 1, ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(2)) });
    await db.SaveChangesAsync();
    await Reject(() => new DonationService(db).Issue(1, new IssueForm { BloodGroup = "A+", Units = 2, Recipient = "Test" }, "test"));
    Check(await db.Inventory.SumAsync(x => x.Units) == 1, "Stock altered");
    Check(await db.InventoryIssues.CountAsync() == 0, "Unexpected audit");
});
await Test("Negative quantities and invalid blood groups are rejected", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    var service = new DonationService(db);
    var f = Form();
    f.Units = -1;
    await Reject(() => service.Complete(1, f));
    f = Form();
    f.BloodGroup = "Z+";
    await Reject(() => service.Complete(1, f));
    await Reject(() => service.Issue(1, new IssueForm { BloodGroup = "O+", Units = -1 }, "test"));
});
await Test("Expired donations cannot create usable stock", async () =>
{
    var (db, c) = await Database();
    await using var connection = c;
    await using var context = db;
    var f = Form();
    f.ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
    await Reject(() => new DonationService(db).Complete(1, f));
    Check(await db.Inventory.CountAsync() == 0, "Expired stock inserted");
});
Console.WriteLine($"{passed} tests passed.");
