
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Customer>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<Customer>().HasIndex(x => x.Phone).IsUnique();
        builder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        builder.Entity<Lead>().HasIndex(x => x.LeadCode).IsUnique();
        builder.Entity<Lead>()
            .HasOne(x => x.ConvertedCustomer)
            .WithMany()
            .HasForeignKey(x => x.ConvertedCustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Opportunity>()
            .HasOne(x => x.Lead)
            .WithMany(x => x.Opportunities)
            .HasForeignKey(x => x.LeadId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FollowUp>()
            .HasOne(x => x.Opportunity)
            .WithMany(x => x.FollowUps)
            .HasForeignKey(x => x.OpportunityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
