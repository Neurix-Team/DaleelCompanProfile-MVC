using Daleel.BAL.Models;

namespace Daleel.Models
{
    public class HomePageViewModel
    {
        public BrandProfileDetailDto? Brand { get; set; }

        public IReadOnlyList<CmsServiceListItemDto> Services { get; set; } = Array.Empty<CmsServiceListItemDto>();

        public IReadOnlyList<CmsProjectListItemDto> Projects { get; set; } = Array.Empty<CmsProjectListItemDto>();

        public IReadOnlyList<TeamMemberListItemDto> TeamMembers { get; set; } = Array.Empty<TeamMemberListItemDto>();

        public IReadOnlyList<TestimonialListItemDto> Testimonials { get; set; } = Array.Empty<TestimonialListItemDto>();

        public IReadOnlyList<ArticleListItem> RecentArticles { get; set; } = Array.Empty<ArticleListItem>();
    }

    public class PlatformsPageViewModel
    {
        public BrandProfileDetailDto? Brand { get; set; }

        public IReadOnlyList<CmsServiceListItemDto> Services { get; set; } = Array.Empty<CmsServiceListItemDto>();

        public IReadOnlyList<CmsProjectListItemDto> Projects { get; set; } = Array.Empty<CmsProjectListItemDto>();
    }

    public class AboutPageViewModel
    {
        public BrandProfileDetailDto? Brand { get; set; }

        public IReadOnlyList<TeamMemberListItemDto> TeamMembers { get; set; } = Array.Empty<TeamMemberListItemDto>();

        public IReadOnlyList<TestimonialListItemDto> Testimonials { get; set; } = Array.Empty<TestimonialListItemDto>();
    }

    public class ContactPageViewModel
    {
        public BrandProfileDetailDto? Brand { get; set; }

        public ContactInquiryViewModel Form { get; set; } = new();
    }
}
