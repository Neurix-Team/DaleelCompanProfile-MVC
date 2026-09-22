using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Daleel.BAL;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO.Compression;


var builder = WebApplication.CreateBuilder(args);

// CRM database (SQL Server) — connection string lives in appsettings / environment.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// CRM authentication & authorization (cookie based, backed by ASP.NET Core Identity).
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/crm/account/login";
    options.LogoutPath = "/crm/account/logout";
    options.AccessDeniedPath = "/crm/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    // Route DataAnnotations display names and validation messages through the same
    // SharedResource the views use, so CMS form labels come from one place instead of
    // a per-model .resx. Models whose Name is not a resource key — the public site's
    // forms — fall through to the literal string, exactly as they rendered before.
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(Daleel.SharedResource)));

// Compress dynamic HTML, JSON, CSS and JavaScript responses. Static assets generated
// by MapStaticAssets can still use their build-time compressed variants.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);

builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Text chat uses the local FastAPI RAG backend. Its URL and request defaults
// live under RagApi in appsettings.Development.json.
builder.Services.AddHttpClient("RagApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});

builder.Services.AddScoped<IGeminiService, GeminiService>();

// CRM & CMS business layers — controllers depend on these interfaces, never on the DbContext.
builder.Services.AddCrmBusinessLayer();
builder.Services.AddCmsBusinessLayer();


var app = builder.Build();

// Create/migrate the CRM database and seed roles + the initial administrator.
// Failures are logged rather than fatal so the public Company Profile site still
// serves even when SQL Server is unreachable.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var seedSection = app.Configuration.GetSection("CrmSeed");
        await services.GetRequiredService<ICrmInitializer>().InitializeAsync(new CrmSeedOptions
        {
            AdminEmail = seedSection["AdminEmail"] ?? string.Empty,
            AdminPassword = seedSection["AdminPassword"] ?? string.Empty,
            AdminFirstName = seedSection["AdminFirstName"] ?? "Daleel",
            AdminLastName = seedSection["AdminLastName"] ?? "Administrator"
        });

        logger.LogInformation("CRM database migrated and seeded successfully.");

        // Seed default CMS dynamic page sections if empty
        var pageSectionService = services.GetRequiredService<IPageSectionService>();
        await pageSectionService.SeedMissingDefaultSectionsAsync();
        var syncedKeys = await pageSectionService.SyncMissingSectionKeysAsync();
        if (syncedKeys > 0)
        {
            logger.LogInformation("Synced {Count} missing default page section keys.", syncedKeys);
        }

        // Retire the placeholder copy the original four pages were seeded with, now that the
        // views actually read these sections. Skips anything an admin has edited.
        var realigned = await pageSectionService.AlignLegacyPlaceholderSectionsAsync();
        if (realigned > 0)
        {
            logger.LogInformation("Realigned {Count} legacy placeholder page sections.", realigned);
        }
        logger.LogInformation("CMS default page sections seeded successfully.");

        // Seed default CMS brand profiles (Daleel and Neurix) if empty
        var brandService = services.GetRequiredService<IBrandProfileService>();
        await brandService.SeedDefaultBrandsIfEmptyAsync();
        logger.LogInformation("CMS default brand profiles seeded successfully.");

        // Seed default CMS brand services if empty
        var serviceService = services.GetRequiredService<ICmsServiceService>();
        await serviceService.SeedDefaultServicesIfEmptyAsync();
        logger.LogInformation("CMS default brand services seeded successfully.");

        // Seed default CMS brand projects if empty
        var projectService = services.GetRequiredService<ICmsProjectService>();
        await projectService.SeedDefaultProjectsIfEmptyAsync();
        logger.LogInformation("CMS default brand projects seeded successfully.");

        // Seed default CMS team members if empty
        var teamService = services.GetRequiredService<ITeamMemberService>();
        await teamService.SeedDefaultTeamIfEmptyAsync();
        logger.LogInformation("CMS default team members seeded successfully.");

        // Seed default CMS testimonials if empty
        var testimonialService = services.GetRequiredService<ITestimonialService>();
        await testimonialService.SeedDefaultTestimonialsIfEmptyAsync();
        logger.LogInformation("CMS default testimonials seeded successfully.");

        // Seed default Navigation links if empty
        var navigationService = services.GetRequiredService<INavigationService>();
        await navigationService.SeedDefaultLinksIfEmptyAsync();
        logger.LogInformation("CMS default navigation links seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "CRM database migration/seeding failed. The CRM area will be unavailable.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

// MapStaticAssets (below) only serves files that existed when the project was built —
// it resolves requests against a build-time manifest. Media-library uploads are written
// to wwwroot/uploads *at runtime*, so they are absent from that manifest and would 404.
// This middleware serves that one directory straight off disk, which is what makes
// uploaded images reachable at the /uploads/... URL stored in the database.
var uploadsPath = Path.Combine(
    app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"),
    "uploads");

// PhysicalFileProvider throws if the directory is missing, and on a fresh deployment
// (or a host that does not preserve empty folders) it may not exist yet.
Directory.CreateDirectory(uploadsPath);

// Restricted to the image types the upload services accept, so an extension the default
// provider does not know about cannot be served with a guessed content type.
var uploadsContentTypes = new FileExtensionContentTypeProvider(
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml"
    });

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ContentTypeProvider = uploadsContentTypes,
    OnPrepareResponse = ctx =>
    {
        // Uploaded files are user-supplied; never let the browser sniff a different type
        // (an uploaded .svg can otherwise become a script execution vector).
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
    }
});

app.UseWebSockets();
app.UseRouting();

var supportedCultures = new[]
{
    new CultureInfo("ar"),
    new CultureInfo("en")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Cookie provider must be FIRST so it takes priority over browser accept-language
localizationOptions.RequestCultureProviders.Insert(0,
    new CookieRequestCultureProvider());

app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers(); // API controllers (e.g. GeminiController)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.Run();
