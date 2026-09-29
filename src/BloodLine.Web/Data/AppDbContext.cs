using BloodLine.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<BloodBank> BloodBanks => Set<BloodBank>();
    public DbSet<RegistrationRequest> RegistrationRequests => Set<RegistrationRequest>();
    public DbSet<Donor> Donors => Set<Donor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<InventoryBatch> Inventory => Set<InventoryBatch>();
    public DbSet<InventoryIssue> InventoryIssues => Set<InventoryIssue>();
    public DbSet<BankEvent> Events => Set<BankEvent>();
    public DbSet<BloodNeed> BloodNeeds => Set<BloodNeed>();
    public DbSet<Faq> Faqs => Set<Faq>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Donation>().HasIndex(x => x.AppointmentId).IsUnique();
        b.Entity<Donor>().HasIndex(x => new { x.BloodBankId, x.Email }).IsUnique();
        b.Entity<Appointment>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<InventoryBatch>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<AppUser>().HasOne(x => x.BloodBank).WithMany().HasForeignKey(x => x.BloodBankId).OnDelete(DeleteBehavior.Restrict);
    }
}
