using Daleel.BAL.Services;
using Daleel.BAL.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Daleel.BAL
{
    /// <summary>
    /// Registration for the CRM business layer, so the web project wires the layer in
    /// with one call instead of knowing every implementation type.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCrmBusinessLayer(this IServiceCollection services)
        {
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<ILeadService, LeadService>();
            services.AddScoped<IDealService, DealService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ICrmInitializer, CrmInitializer>();

            return services;
        }

        public static IServiceCollection AddCmsBusinessLayer(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<IBrandProfileService, BrandProfileService>();
            services.AddScoped<ICmsServiceService, CmsServiceService>();
            services.AddScoped<ICmsProjectService, CmsProjectService>();
            services.AddScoped<ITeamMemberService, TeamMemberService>();
            services.AddScoped<ITestimonialService, TestimonialService>();
            services.AddScoped<IArticleService, ArticleService>();
            services.AddScoped<IPageSectionService, PageSectionService>();
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IMediaLibraryService, MediaLibraryService>();
            services.AddScoped<INavigationService, NavigationService>();

            return services;
        }
    }
}
