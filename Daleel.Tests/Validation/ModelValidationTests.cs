using System.ComponentModel.DataAnnotations;
using Daleel.Models;

namespace Daleel.Tests.Validation
{
    public class ModelValidationTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void LoginViewModel_MissingEmailAndPassword_FailsValidation()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "",
                Password = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(LoginViewModel.Email)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(LoginViewModel.Password)));
        }

        [Fact]
        public void LoginViewModel_InvalidEmail_FailsValidation()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "invalid-email-format",
                Password = "ValidPassword123!"
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(LoginViewModel.Email)));
        }

        [Fact]
        public void ArticleFormViewModel_MissingTitles_FailsValidation()
        {
            // Arrange
            var model = new ArticleFormViewModel
            {
                TitleEn = "",
                TitleAr = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ArticleFormViewModel.TitleEn)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ArticleFormViewModel.TitleAr)));
        }

        [Fact]
        public void ArticleFormViewModel_MissingContent_FailsValidation()
        {
            var model = new ArticleFormViewModel
            {
                TitleEn = "English title",
                TitleAr = "Arabic title",
                BodyHtmlEn = "",
                BodyHtmlAr = ""
            };

            var errors = ValidateModel(model);

            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ArticleFormViewModel.BodyHtmlEn)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ArticleFormViewModel.BodyHtmlAr)));
        }

        [Fact]
        public void LeadFormViewModel_MissingFirstOrLastName_FailsValidation()
        {
            // Arrange
            var model = new LeadFormViewModel
            {
                FirstName = "",
                LastName = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(LeadFormViewModel.FirstName)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(LeadFormViewModel.LastName)));
        }

        [Fact]
        public void CompanyFormViewModel_MissingName_FailsValidation()
        {
            // Arrange
            var model = new CompanyFormViewModel
            {
                Name = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(CompanyFormViewModel.Name)));
        }

        [Theory]
        [InlineData("not-a-url", false)]
        [InlineData("www.example.com", false)]
        [InlineData("https://www.example.com", true)]
        [InlineData(null, true)]
        public void CompanyFormViewModel_Website_RequiresAbsoluteUrl(string? website, bool isValid)
        {
            // Arrange
            var model = new CompanyFormViewModel
            {
                Name = "Example Company",
                Website = website
            };

            // Act
            var errors = ValidateModel(model);
            var websiteErrors = errors
                .Where(e => e.MemberNames.Contains(nameof(CompanyFormViewModel.Website)))
                .ToList();

            // Assert
            if (isValid)
            {
                websiteErrors.Should().BeEmpty();
            }
            else
            {
                websiteErrors.Should().ContainSingle();
            }
        }

        [Theory]
        [InlineData("3M")]
        [InlineData("AT&T")]
        [InlineData("Company 2")]
        [InlineData("Smith & Sons (MENA)")]
        public void ScheduleDemoViewModel_ValidBusinessCompanyNames_PassValidation(string companyName)
        {
            var model = new ScheduleDemoViewModel
            {
                FirstName = "John",
                LastName = "Smith",
                WorkEmail = "john@example.com",
                Company = companyName,
                JobTitle = "CTO"
            };

            var errors = ValidateModel(model);

            errors.Should().NotContain(e =>
                e.MemberNames.Contains(nameof(ScheduleDemoViewModel.Company)));
        }

        [Fact]
        public void ContactFormViewModel_MissingFirstOrLastName_FailsValidation()
        {
            // Arrange
            var model = new ContactFormViewModel
            {
                FirstName = "",
                LastName = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ContactFormViewModel.FirstName)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ContactFormViewModel.LastName)));
        }

        [Fact]
        public void DealFormViewModel_MissingNameOrInvalidValue_FailsValidation()
        {
            // Arrange
            var model = new DealFormViewModel
            {
                Name = "",
                Value = -500m
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(DealFormViewModel.Name)));
        }

        [Fact]
        public void TaskFormViewModel_MissingTitle_FailsValidation()
        {
            // Arrange
            var model = new TaskFormViewModel
            {
                Title = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(TaskFormViewModel.Title)));
        }

        [Fact]
        public void ContactInquiryViewModel_MissingRequiredFields_FailsValidation()
        {
            // Arrange
            var model = new ContactInquiryViewModel
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                Message = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ContactInquiryViewModel.FirstName)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ContactInquiryViewModel.LastName)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ContactInquiryViewModel.Email)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ContactInquiryViewModel.Message)));
        }

        [Fact]
        public void BrandProfileFormViewModel_MissingNames_FailsValidation()
        {
            // Arrange
            var model = new BrandProfileFormViewModel
            {
                NameEn = "",
                NameAr = ""
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(BrandProfileFormViewModel.NameEn)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(BrandProfileFormViewModel.NameAr)));
        }

        [Fact]
        public void BrandProfileFormViewModel_InvalidHexColor_FailsValidation()
        {
            // Arrange
            var model = new BrandProfileFormViewModel
            {
                NameEn = "Daleel",
                NameAr = "دليل",
                PrimaryColor = "invalid-color-code"
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(BrandProfileFormViewModel.PrimaryColor)));
        }

        [Fact]
        public void BrandProfileFormViewModel_ValidHexColors_PassesValidation()
        {
            // Arrange
            var model = new BrandProfileFormViewModel
            {
                NameEn = "Daleel",
                NameAr = "دليل",
                PrimaryColor = "#00B2EC",
                SecondaryColor = "#10B981",
                AccentColor = "#F9A01B"
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void CreateCmsUserViewModel_MissingRequiredFields_FailsValidation()
        {
            // Arrange
            var model = new CreateCmsUserViewModel
            {
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                Password = "123", // Too short
                ConfirmPassword = "456" // Mismatched
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(CreateCmsUserViewModel.FirstName)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(CreateCmsUserViewModel.LastName)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(CreateCmsUserViewModel.Email)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(CreateCmsUserViewModel.Password)));
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(CreateCmsUserViewModel.ConfirmPassword)));
        }

        [Fact]
        public void CreateCmsUserViewModel_ValidData_PassesValidation()
        {
            // Arrange
            var model = new CreateCmsUserViewModel
            {
                FirstName = "Ahmad",
                LastName = "Al-Sayed",
                Email = "ahmad@aidaleel.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = "Admin"
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ResetUserPasswordViewModel_MismatchedPasswords_FailsValidation()
        {
            // Arrange
            var model = new ResetUserPasswordViewModel
            {
                UserId = "user-123",
                Email = "test@aidaleel.com",
                FullName = "Test User",
                NewPassword = "Password123!",
                ConfirmPassword = "DifferentPassword!"
            };

            // Act
            var errors = ValidateModel(model);

            // Assert
            errors.Should().Contain(e => e.MemberNames.Contains(nameof(ResetUserPasswordViewModel.ConfirmPassword)));
        }
    }
}
