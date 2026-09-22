using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Daleel.DAL.Data
{
    /// <summary>
    /// Single database context for the application (Identity today, CRM entities in later phases).
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies => Set<Company>();

        public DbSet<Contact> Contacts => Set<Contact>();

        public DbSet<Lead> Leads => Set<Lead>();

        public DbSet<Deal> Deals => Set<Deal>();

        public DbSet<Activity> Activities => Set<Activity>();

        public DbSet<CrmTask> CrmTasks => Set<CrmTask>();

        public DbSet<Article> Articles => Set<Article>();

        public DbSet<PageSection> PageSections => Set<PageSection>();

        public DbSet<BrandProfile> BrandProfiles => Set<BrandProfile>();

        public DbSet<CmsService> CmsServices => Set<CmsService>();

        public DbSet<CmsProject> CmsProjects => Set<CmsProject>();

        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

        public DbSet<Testimonial> Testimonials => Set<Testimonial>();
        public DbSet<NavigationLink> NavigationLinks => Set<NavigationLink>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            });

            builder.Entity<Company>(entity =>
            {
                entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
                entity.Property(c => c.Industry).HasMaxLength(150);
                entity.Property(c => c.Website).HasMaxLength(300);
                entity.Property(c => c.Phone).HasMaxLength(30);
                entity.Property(c => c.Email).HasMaxLength(254);
                entity.Property(c => c.Address).HasMaxLength(300);
                entity.Property(c => c.City).HasMaxLength(100);
                entity.Property(c => c.Country).HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(2000);

                // Stored as text so the schema does not depend on enum member ordering.
                entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

                // Not unique: franchises/subsidiaries legitimately share a name.
                entity.HasIndex(c => c.Name);
            });

            builder.Entity<Contact>(entity =>
            {
                entity.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(c => c.LastName).HasMaxLength(100).IsRequired();
                entity.Property(c => c.JobTitle).HasMaxLength(150);
                entity.Property(c => c.Email).HasMaxLength(254);
                entity.Property(c => c.Phone).HasMaxLength(30);
                entity.Property(c => c.Notes).HasMaxLength(2000);
                entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

                // Deleting a company orphans its contacts rather than deleting them.
                entity.HasOne(c => c.Company)
                      .WithMany(co => co.Contacts)
                      .HasForeignKey(c => c.CompanyId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(c => c.CompanyId);
                entity.HasIndex(c => c.LastName);
            });

            builder.Entity<Lead>(entity =>
            {
                entity.Property(l => l.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(l => l.LastName).HasMaxLength(100).IsRequired();
                entity.Property(l => l.Email).HasMaxLength(254);
                entity.Property(l => l.Phone).HasMaxLength(30);
                entity.Property(l => l.CompanyName).HasMaxLength(200);
                entity.Property(l => l.JobTitle).HasMaxLength(150);
                entity.Property(l => l.Notes).HasMaxLength(4000);

                entity.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(l => l.Source).HasConversion<string>().HasMaxLength(30);

                // Removing a staff account unassigns their leads rather than deleting them.
                entity.HasOne(l => l.AssignedTo)
                      .WithMany()
                      .HasForeignKey(l => l.AssignedToId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // Deleting the converted contact leaves the lead intact (and re-convertible)
                // rather than cascading the delete back onto its history.
                entity.HasOne(l => l.ConvertedContact)
                      .WithMany()
                      .HasForeignKey(l => l.ConvertedContactId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => l.AssignedToId);
                entity.HasIndex(l => l.IsArchived);
                entity.HasIndex(l => l.ConvertedContactId);
            });

            builder.Entity<Deal>(entity =>
            {
                entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
                entity.Property(d => d.Description).HasMaxLength(2000);

                // Money: fixed precision so totals never drift the way a float would.
                entity.Property(d => d.Value).HasPrecision(18, 2);
                entity.Property(d => d.Currency).HasMaxLength(3).IsRequired();

                entity.Property(d => d.Stage).HasConversion<string>().HasMaxLength(20);

                // Losing a company, contact or staff account must never delete the deal —
                // its value is pipeline history.
                entity.HasOne(d => d.Company)
                      .WithMany()
                      .HasForeignKey(d => d.CompanyId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.PrimaryContact)
                      .WithMany()
                      .HasForeignKey(d => d.PrimaryContactId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.AssignedTo)
                      .WithMany()
                      .HasForeignKey(d => d.AssignedToId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(d => d.Stage);
                entity.HasIndex(d => d.CompanyId);
                entity.HasIndex(d => d.PrimaryContactId);
                entity.HasIndex(d => d.AssignedToId);
                entity.HasIndex(d => d.ExpectedCloseDate);
            });

            builder.Entity<Activity>(entity =>
            {
                entity.Property(a => a.Subject).HasMaxLength(200).IsRequired();
                entity.Property(a => a.Notes).HasMaxLength(4000);

                entity.Property(a => a.EntityType).HasConversion<string>().HasMaxLength(20);
                entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);

                // Removing a staff account keeps the history, just without an author.
                entity.HasOne(a => a.CreatedBy)
                      .WithMany()
                      .HasForeignKey(a => a.CreatedByUserId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // The lookup every detail page makes: "activities for this record".
                entity.HasIndex(a => new { a.EntityType, a.EntityId });
                entity.HasIndex(a => a.OccurredAt);
            });

            builder.Entity<CrmTask>(entity =>
            {
                entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
                entity.Property(t => t.Description).HasMaxLength(2000);

                entity.Property(t => t.EntityType).HasConversion<string>().HasMaxLength(20);
                entity.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
                entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasOne(t => t.AssignedTo)
                      .WithMany()
                      .HasForeignKey(t => t.AssignedToId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(t => new { t.EntityType, t.EntityId });
                entity.HasIndex(t => t.AssignedToId);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.DueDate);
            });

            builder.Entity<Article>(entity =>
            {
                entity.Property(a => a.Slug).HasMaxLength(250).IsRequired();
                entity.Property(a => a.TitleEn).HasMaxLength(300).IsRequired();
                entity.Property(a => a.TitleAr).HasMaxLength(300).IsRequired();
                entity.Property(a => a.SummaryEn).HasMaxLength(1000);
                entity.Property(a => a.SummaryAr).HasMaxLength(1000);
                entity.Property(a => a.CoverImagePath).HasMaxLength(500);
                entity.Property(a => a.Category).HasMaxLength(100);
                entity.Property(a => a.Tags).HasMaxLength(500);
                entity.Property(a => a.AuthorName).HasMaxLength(150).IsRequired();

                entity.Property(a => a.MetaTitleEn).HasMaxLength(200);
                entity.Property(a => a.MetaTitleAr).HasMaxLength(200);
                entity.Property(a => a.MetaDescriptionEn).HasMaxLength(500);
                entity.Property(a => a.MetaDescriptionAr).HasMaxLength(500);
                entity.Property(a => a.CanonicalUrl).HasMaxLength(500);

                entity.HasIndex(a => a.Slug).IsUnique();
                entity.HasIndex(a => a.IsPublished);
                entity.HasIndex(a => a.PublishedAt);
                entity.HasIndex(a => a.ScheduledPublishAt);
                entity.HasIndex(a => a.Category);
            });

            builder.Entity<PageSection>(entity =>
            {
                entity.Property(p => p.PageKey).HasMaxLength(100).IsRequired();
                entity.Property(p => p.SectionKey).HasMaxLength(100).IsRequired();
                entity.Property(p => p.DataType).HasMaxLength(50).IsRequired();

                entity.HasIndex(p => new { p.PageKey, p.SectionKey }).IsUnique();
            });

            builder.Entity<BrandProfile>(entity =>
            {
                entity.Property(b => b.Slug).HasMaxLength(100).IsRequired();
                entity.Property(b => b.NameEn).HasMaxLength(200).IsRequired();
                entity.Property(b => b.NameAr).HasMaxLength(200).IsRequired();
                entity.Property(b => b.TaglineEn).HasMaxLength(300);
                entity.Property(b => b.TaglineAr).HasMaxLength(300);
                entity.Property(b => b.DescriptionEn).HasMaxLength(4000);
                entity.Property(b => b.DescriptionAr).HasMaxLength(4000);
                entity.Property(b => b.LogoPath).HasMaxLength(500);
                entity.Property(b => b.LogoDarkPath).HasMaxLength(500);
                entity.Property(b => b.FaviconPath).HasMaxLength(500);
                entity.Property(b => b.PrimaryColor).HasMaxLength(50);
                entity.Property(b => b.SecondaryColor).HasMaxLength(50);
                entity.Property(b => b.AccentColor).HasMaxLength(50);
                entity.Property(b => b.MetaTitleEn).HasMaxLength(200);
                entity.Property(b => b.MetaTitleAr).HasMaxLength(200);
                entity.Property(b => b.MetaDescriptionEn).HasMaxLength(500);
                entity.Property(b => b.MetaDescriptionAr).HasMaxLength(500);
                entity.Property(b => b.GoogleAnalyticsId).HasMaxLength(50);
                entity.Property(b => b.LinkedInUrl).HasMaxLength(500);
                entity.Property(b => b.TwitterUrl).HasMaxLength(500);
                entity.Property(b => b.FacebookUrl).HasMaxLength(500);
                entity.Property(b => b.InstagramUrl).HasMaxLength(500);
                entity.Property(b => b.Email).HasMaxLength(254);
                entity.Property(b => b.Phone).HasMaxLength(50);
                entity.Property(b => b.Address).HasMaxLength(500);
                entity.Property(b => b.Website).HasMaxLength(500);

                entity.HasQueryFilter(b => !b.IsDeleted);

                entity.HasIndex(b => b.Slug).IsUnique();
                entity.HasIndex(b => b.IsPublished);
            });

            builder.Entity<CmsService>(entity =>
            {
                entity.Property(s => s.Slug).HasMaxLength(150).IsRequired();
                entity.Property(s => s.NameEn).HasMaxLength(200).IsRequired();
                entity.Property(s => s.NameAr).HasMaxLength(200).IsRequired();
                entity.Property(s => s.ShortDescEn).HasMaxLength(500);
                entity.Property(s => s.ShortDescAr).HasMaxLength(500);
                entity.Property(s => s.DescriptionEn).HasMaxLength(4000);
                entity.Property(s => s.DescriptionAr).HasMaxLength(4000);
                entity.Property(s => s.IconName).HasMaxLength(100);
                entity.Property(s => s.ImagePath).HasMaxLength(500);

                entity.HasOne(s => s.Brand)
                      .WithMany()
                      .HasForeignKey(s => s.BrandId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(s => !s.IsDeleted);

                entity.HasIndex(s => new { s.BrandId, s.Slug }).IsUnique();
                entity.HasIndex(s => s.BrandId);
                entity.HasIndex(s => s.IsPublished);
                entity.HasIndex(s => s.DisplayOrder);
            });

            builder.Entity<CmsProject>(entity =>
            {
                entity.Property(p => p.Slug).HasMaxLength(150).IsRequired();
                entity.Property(p => p.NameEn).HasMaxLength(200).IsRequired();
                entity.Property(p => p.NameAr).HasMaxLength(200).IsRequired();
                entity.Property(p => p.ShortDescEn).HasMaxLength(500);
                entity.Property(p => p.ShortDescAr).HasMaxLength(500);
                entity.Property(p => p.DescriptionEn).HasMaxLength(4000);
                entity.Property(p => p.DescriptionAr).HasMaxLength(4000);
                entity.Property(p => p.ClientNameEn).HasMaxLength(200);
                entity.Property(p => p.ClientNameAr).HasMaxLength(200);
                entity.Property(p => p.ProjectUrl).HasMaxLength(500);
                entity.Property(p => p.ImagePath).HasMaxLength(500);

                entity.HasOne(p => p.Brand)
                      .WithMany()
                      .HasForeignKey(p => p.BrandId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(p => !p.IsDeleted);

                entity.HasIndex(p => new { p.BrandId, p.Slug }).IsUnique();
                entity.HasIndex(p => p.BrandId);
                entity.HasIndex(p => p.IsPublished);
                entity.HasIndex(p => p.DisplayOrder);
            });

            builder.Entity<TeamMember>(entity =>
            {
                entity.Property(t => t.Slug).HasMaxLength(150).IsRequired();
                entity.Property(t => t.NameEn).HasMaxLength(200).IsRequired();
                entity.Property(t => t.NameAr).HasMaxLength(200).IsRequired();
                entity.Property(t => t.TitleEn).HasMaxLength(200);
                entity.Property(t => t.TitleAr).HasMaxLength(200);
                entity.Property(t => t.BioEn).HasMaxLength(2000);
                entity.Property(t => t.BioAr).HasMaxLength(2000);
                entity.Property(t => t.PhotoPath).HasMaxLength(500);
                entity.Property(t => t.Email).HasMaxLength(254);
                entity.Property(t => t.LinkedInUrl).HasMaxLength(500);

                entity.HasOne(t => t.Brand)
                      .WithMany()
                      .HasForeignKey(t => t.BrandId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(t => !t.IsDeleted);

                entity.HasIndex(t => new { t.BrandId, t.Slug }).IsUnique();
                entity.HasIndex(t => t.BrandId);
                entity.HasIndex(t => t.IsPublished);
                entity.HasIndex(t => t.DisplayOrder);
            });

            builder.Entity<Testimonial>(entity =>
            {
                entity.Property(t => t.CustomerNameEn).HasMaxLength(200).IsRequired();
                entity.Property(t => t.CustomerNameAr).HasMaxLength(200).IsRequired();
                entity.Property(t => t.CompanyNameEn).HasMaxLength(200);
                entity.Property(t => t.CompanyNameAr).HasMaxLength(200);
                entity.Property(t => t.RoleTitleEn).HasMaxLength(200);
                entity.Property(t => t.RoleTitleAr).HasMaxLength(200);
                entity.Property(t => t.ContentEn).HasMaxLength(2000).IsRequired();
                entity.Property(t => t.ContentAr).HasMaxLength(2000).IsRequired();
                entity.Property(t => t.PhotoPath).HasMaxLength(500);

                entity.HasOne(t => t.Brand)
                      .WithMany()
                      .HasForeignKey(t => t.BrandId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(t => !t.IsDeleted);

                entity.HasIndex(t => t.BrandId);
                entity.HasIndex(t => t.IsPublished);
                entity.HasIndex(t => t.DisplayOrder);
            });

            builder.Entity<NavigationLink>(entity =>
            {
                entity.Property(n => n.LabelEn).HasMaxLength(100).IsRequired();
                entity.Property(n => n.LabelAr).HasMaxLength(100).IsRequired();
                entity.Property(n => n.Url).HasMaxLength(500).IsRequired();
                entity.Property(n => n.Location).IsRequired();

                entity.HasIndex(n => new { n.Location, n.Section, n.SortOrder });
                entity.HasIndex(n => n.IsActive);
            });
        }
    }
}
