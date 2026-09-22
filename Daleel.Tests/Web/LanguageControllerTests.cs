using Daleel.Controllers;
using Daleel.Tests.Common;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Tests.Web
{
    public class LanguageControllerTests
    {
        [Fact]
        public void SetLanguage_SetsCultureCookie_AndRedirectsToLocalUrl()
        {
            // Arrange
            var controller = new LanguageController().WithTestContext();

            // Act
            var result = controller.SetLanguage("ar", "/crm/leads");

            // Assert
            var localRedirect = result.Should().BeOfType<LocalRedirectResult>().Subject;
            localRedirect.Url.Should().Be("/crm/leads");

            var cookies = controller.Response.Headers["Set-Cookie"].ToString();
            cookies.Should().Contain(CookieRequestCultureProvider.DefaultCookieName);
            cookies.Should().Contain("ar");
        }

        [Theory]
        [InlineData("https://evil.com")]
        [InlineData("http://attacker.com/malicious")]
        [InlineData("//evil.com")]
        [InlineData(null)]
        [InlineData("")]
        public void SetLanguage_NonLocalUrl_SafelyRedirectsToHomeIndex(string? returnUrl)
        {
            // Arrange
            var controller = new LanguageController().WithTestContext();

            // Act
            var result = controller.SetLanguage("en", returnUrl!);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be("Index");
            redirect.ControllerName.Should().Be("Home");
        }

        [Fact]
        public void SetArabic_SetsArabicCookie_AndRedirectsToReturnUrl()
        {
            // Arrange
            var controller = new LanguageController().WithTestContext();

            // Act
            var result = controller.SetArabic("/Home/Shop");

            // Assert
            var localRedirect = result.Should().BeOfType<LocalRedirectResult>().Subject;
            localRedirect.Url.Should().Be("/Home/Shop");

            var cookies = controller.Response.Headers["Set-Cookie"].ToString();
            cookies.Should().Contain("ar");
        }

        [Fact]
        public void SetEnglish_SetsEnglishCookie_AndRedirectsToReturnUrl()
        {
            // Arrange
            var controller = new LanguageController().WithTestContext();

            // Act
            var result = controller.SetEnglish("/Home/About");

            // Assert
            var localRedirect = result.Should().BeOfType<LocalRedirectResult>().Subject;
            localRedirect.Url.Should().Be("/Home/About");

            var cookies = controller.Response.Headers["Set-Cookie"].ToString();
            cookies.Should().Contain("en");
        }
    }
}
