namespace Daleel.Models
{
    /// <summary>
    /// View-only shape handed to the shared _Pager partial. Kept non-generic so a
    /// single partial serves every CRM list page. Filters are not listed here — the
    /// partial carries them across pages by reading the current query string.
    /// </summary>
    public class PagerViewModel
    {
        public int Page { get; init; }

        public int TotalPages { get; init; }

        public int TotalCount { get; init; }
    }
}
