namespace Daleel.BAL.Services
{
    /// <summary>Normalisation shared by the CRM services.</summary>
    internal static class Text
    {
        /// <summary>Trims free text, collapsing blank input to null so the column stays NULL.</summary>
        public static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
