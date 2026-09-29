using BloodLine.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider services, IConfiguration config, IWebHostEnvironment env)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Admin, Roles.Manager, Roles.Staff })
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));
        var users = services.GetRequiredService<UserManager<AppUser>>();
        if (await users.Users.AnyAsync())
            return;
        var password = config["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("First run: set Seed__Password to a strong password of at least 12 characters, including uppercase, lowercase, number and symbol. See README.md.");
        async Task Add(string email, string name, string role, int? bank)
        {
            var u = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = name, BloodBankId = bank, Position = role };
            var result = await users.CreateAsync(u, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            var assigned = await users.AddToRoleAsync(u, role);
            if (!assigned.Succeeded)
                throw new InvalidOperationException("Could not assign initial role.");
        }
        // Keep bootstrap atomic: an invalid password must not leave a partially seeded database.
        await using var tx = await db.Database.BeginTransactionAsync();
        await Add(config["Seed:AdminEmail"] ?? "admin@example.test", "BloodLine Administrator", Roles.Admin, null);
        if (env.IsDevelopment() && config.GetValue<bool>("Seed:DemoData"))
        {
            var bank = new BloodBank { Name = "BloodLine Demonstration Center", Email = "center@example.test", Phone = "+10000000000", Latitude = 25.35, Longitude = 55.42 };
            db.BloodBanks.Add(bank);
            await db.SaveChangesAsync();
            await Add("manager@example.test", "Demo Manager", Roles.Manager, bank.Id);
            await Add("staff@example.test", "Demo Staff", Roles.Staff, bank.Id);
            var donor = new Donor { Name = "Sample Donor", Email = "donor@example.test", BloodBankId = bank.Id, BloodGroup = "O+", Weight = 65, IsVolunteer = true };
            db.Donors.Add(donor);
            await db.SaveChangesAsync();
            db.Appointments.Add(new Appointment { BloodBankId = bank.Id, DonorId = donor.Id, ScheduledAt = DateTime.Today.AddHours(10) });
            db.Inventory.Add(new InventoryBatch { BloodBankId = bank.Id, BloodGroup = "O+", Units = 5, ExpiresOn = DateOnly.FromDateTime(DateTime.Today.AddDays(21)) });
            db.Events.Add(new BankEvent { BloodBankId = bank.Id, Title = "Community donation day", Description = "Fictional event for demonstrating the course project.", Location = "Demonstration center", StartsAt = DateTime.Today.AddDays(7).AddHours(9) });
            db.Faqs.Add(new Faq { Question = "What is BloodLine?", Answer = "A course project for managing blood-bank operations.", CreatedBy = "seed" });
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }
}
