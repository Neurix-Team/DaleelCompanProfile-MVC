using Daleel.BAL.Models;

namespace Daleel.Models
{
    public class PageSectionsEditViewModel
    {
        public string PageKey { get; set; } = string.Empty;

        public List<PageSectionInput> Sections { get; set; } = new();
    }

    public class BlogIndexViewModel
    {
        public PagedResult<ArticleListItem> Articles { get; set; } = new();

        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

        public string? SelectedCategory { get; set; }

        public string? SelectedTag { get; set; }

        public string? SearchTerm { get; set; }
    }
}
