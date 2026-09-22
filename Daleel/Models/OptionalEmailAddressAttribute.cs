using System.ComponentModel.DataAnnotations;

namespace Daleel.Models
{
    /// <summary>
    /// Validates e-mail format only when a value is actually supplied.
    /// Plain [EmailAddress] tolerates null but rejects an empty string, and an empty
    /// text input binds to string.Empty — so it wrongly fails blank optional fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class OptionalEmailAddressAttribute : ValidationAttribute
    {
        private static readonly EmailAddressAttribute Inner = new();

        public override bool IsValid(object? value)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return Inner.IsValid(text);
        }
    }
}
