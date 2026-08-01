using DTO.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;


public class AppDbContext : DbContext
{
    public DbSet<ClientEntity> Clients { get; set; } = default!;
    public DbSet<SenderIdentityEntity> SenderIdentities { get; set; } = default!;
    public DbSet<TemplateEntity> Templates { get; set; } = default!;
    public DbSet<EmailEntity> Emails { get; set; } = default!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureClients(modelBuilder);
        ConfigureSenderIdentities(modelBuilder);
        ConfigureTemplates(modelBuilder);
        ConfigureEmails(modelBuilder);
    }

    private static void ConfigureClients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientEntity>()
            .HasIndex(x => x.ServiceName)
            .IsUnique();

        modelBuilder.Entity<ClientEntity>()
            .Property(x => x.ServiceName)
            .IsRequired();

        modelBuilder.Entity<ClientEntity>()
            .Property(x => x.DisplayName)
            .IsRequired();
    }

    private static void ConfigureSenderIdentities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SenderIdentityEntity>()
            .HasIndex(x => new { x.ClientId, x.EmailType, x.Tenant })
            .IsUnique();

        modelBuilder.Entity<SenderIdentityEntity>()
            .Property(x => x.EmailType)
            .IsRequired();

        modelBuilder.Entity<SenderIdentityEntity>()
            .Property(x => x.FromAddress)
            .IsRequired();

        modelBuilder.Entity<SenderIdentityEntity>()
            .Property(x => x.DisplayName)
            .IsRequired();

        modelBuilder.Entity<SenderIdentityEntity>()
            .HasOne<ClientEntity>()
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureTemplates(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TemplateEntity>()
            .HasIndex(x => new { x.SenderIdentityId, x.LanguageCode })
            .IsUnique();

        modelBuilder.Entity<TemplateEntity>()
            .Property(x => x.LanguageCode)
            .IsRequired();

        modelBuilder.Entity<TemplateEntity>()
            .Property(x => x.SubjectTemplate)
            .IsRequired();

        modelBuilder.Entity<TemplateEntity>()
            .Property(x => x.HtmlBodyTemplate)
            .IsRequired();

        modelBuilder.Entity<TemplateEntity>()
            .HasOne<SenderIdentityEntity>()
            .WithMany()
            .HasForeignKey(x => x.SenderIdentityId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureEmails(ModelBuilder modelBuilder)
    {
        // The idempotency guard: the envelope id is unique, so a duplicate delivery cannot create a
        // second row (and a second email). Inserting this row before sending is the dedup itself.
        modelBuilder.Entity<EmailEntity>()
            .HasIndex(x => x.MessageId)
            .IsUnique();

        modelBuilder.Entity<EmailEntity>()
            .HasIndex(x => new { x.ServiceName, x.EmailType });

        modelBuilder.Entity<EmailEntity>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<EmailEntity>()
            .Property(x => x.MessageId)
            .IsRequired();

        modelBuilder.Entity<EmailEntity>()
            .Property(x => x.ServiceName)
            .IsRequired();

        modelBuilder.Entity<EmailEntity>()
            .Property(x => x.EmailType)
            .IsRequired();

        modelBuilder.Entity<EmailEntity>()
            .Property(x => x.ToRecipients)
            .IsRequired();
    }
}