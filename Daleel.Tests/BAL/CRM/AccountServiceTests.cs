using System.Security.Claims;
using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Daleel.Tests.BAL.CRM
{
    public class AccountServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IHttpContextAccessor> _mockContextAccessor;
        private readonly Mock<IUserClaimsPrincipalFactory<ApplicationUser>> _mockClaimsFactory;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<ILogger<AccountService>> _mockLogger;
        private readonly AccountService _service;

        public AccountServiceTests()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _mockContextAccessor = new Mock<IHttpContextAccessor>();
            _mockClaimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            options.Setup(o => o.Value).Returns(new IdentityOptions());
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                _mockContextAccessor.Object,
                _mockClaimsFactory.Object,
                options.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object);

            _mockLogger = new Mock<ILogger<AccountService>>();
            _service = new AccountService(_mockSignInManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SignInAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync("admin@daleel.sa", "P@ssword123", true, true))
                .ReturnsAsync(SignInResult.Success);

            // Act
            var outcome = await _service.SignInAsync("admin@daleel.sa", "P@ssword123", true);

            // Assert
            outcome.Should().Be(SignInOutcome.Success);
        }

        [Fact]
        public async Task SignInAsync_LockedOutUser_ReturnsLockedOut()
        {
            // Arrange
            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync("locked@daleel.sa", "wrongpass", false, true))
                .ReturnsAsync(SignInResult.LockedOut);

            // Act
            var outcome = await _service.SignInAsync("locked@daleel.sa", "wrongpass", false);

            // Assert
            outcome.Should().Be(SignInOutcome.LockedOut);
        }

        [Fact]
        public async Task SignInAsync_InvalidPassword_ReturnsInvalidCredentials()
        {
            // Arrange
            _mockSignInManager
                .Setup(s => s.PasswordSignInAsync("user@daleel.sa", "wrongpass", false, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var outcome = await _service.SignInAsync("user@daleel.sa", "wrongpass", false);

            // Assert
            outcome.Should().Be(SignInOutcome.InvalidCredentials);
        }

        [Fact]
        public async Task SignOutAsync_CallsSignInManagerSignOut()
        {
            // Arrange
            _mockSignInManager
                .Setup(s => s.SignOutAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _service.SignOutAsync();

            // Assert
            _mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);
        }

        [Fact]
        public void IsSignedIn_DelegatesToSignInManager()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "mock"));
            _mockSignInManager
                .Setup(s => s.IsSignedIn(principal))
                .Returns(true);

            // Act
            var result = _service.IsSignedIn(principal);

            // Assert
            result.Should().BeTrue();
        }
    }
}
