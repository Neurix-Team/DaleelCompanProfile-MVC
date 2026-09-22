namespace Daleel.BAL.Models
{
    /// <summary>
    /// A business-rule failure tied to an input field. The presentation layer decides
    /// how to show it (for MVC, by pushing it into ModelState).
    /// </summary>
    /// <param name="Field">Name of the offending input, or empty for a form-level error.</param>
    public sealed record ServiceError(string Field, string Message);

    /// <summary>
    /// Outcome of a service call: the value, "no such record", or field-level errors.
    /// Lets controllers stay free of data-access concerns while still reporting
    /// precise validation feedback.
    /// </summary>
    public class ServiceResult<T>
    {
        public T? Value { get; private init; }

        /// <summary>True when the requested record does not exist (maps to HTTP 404).</summary>
        public bool NotFound { get; private init; }

        public IReadOnlyList<ServiceError> Errors { get; private init; } = Array.Empty<ServiceError>();

        public bool Succeeded => !NotFound && Errors.Count == 0;

        public string? FirstErrorMessage => Errors.Count > 0 ? Errors[0].Message : null;

        public static ServiceResult<T> Success(T value) => new() { Value = value };

        public static ServiceResult<T> Missing() => new() { NotFound = true };

        public static ServiceResult<T> Invalid(params ServiceError[] errors) =>
            new() { Errors = errors };

        public static ServiceResult<T> Invalid(string field, string message) =>
            Invalid(new ServiceError(field, message));
    }

    public class ServiceResult
    {
        public bool NotFound { get; private init; }

        public IReadOnlyList<ServiceError> Errors { get; private init; } = Array.Empty<ServiceError>();

        public bool Succeeded => !NotFound && Errors.Count == 0;

        public string? FirstErrorMessage => Errors.Count > 0 ? Errors[0].Message : null;

        public static ServiceResult Success() => new();

        public static ServiceResult Missing() => new() { NotFound = true };

        public static ServiceResult Invalid(params ServiceError[] errors) =>
            new() { Errors = errors };

        public static ServiceResult Invalid(string field, string message) =>
            Invalid(new ServiceError(field, message));
    }
}
