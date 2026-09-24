using Daleel.BAL.Models;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Daleel.Tests.Web
{
    public class CreateUserCommandTests : IDisposable
    {
        private const string Email = "new.admin@example.com";
        private const string StrongPassword = "Str0ngPassw0rd";

        private readonly ServiceProvider _root;
        private readonly IServiceScope _scope;

        public CreateUserCommandTests()
        {
            var services = new ServiceCollection();
            var dbName = Guid.NewGuid().ToString();
            services.AddLogging();
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
            services.AddIdentity<ApplicationUser, IdentityRole>(o =>
                {
                    o.Password.RequiredLength = 8;
                    o.Password.RequireNonAlphanumeric = false;
                    o.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            _root = services.BuildServiceProvider();
            _scope = _root.CreateScope();
        }

        private UserManager<ApplicationUser> Users => _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        private Task<int> Run(string[] args, params string[] secrets)
        {
            var queue = new Queue<string>(secrets);
            return CreateUserCommand.RunAsync(_scope.ServiceProvider, args, _ => queue.Count > 0 ? queue.Dequeue() : string.Empty, new StringWriter());
        }

        [Fact]
        public void IsInvocation_OnlyMatchesTheCommandName()
        {
            CreateUserCommand.IsInvocation(new[] { "create-user", Email }).Should().BeTrue();
            CreateUserCommand.IsInvocation(Array.Empty<string>()).Should().BeFalse();
            CreateUserCommand.IsInvocation(new[] { "--urls", "http://+:80" }).Should().BeFalse();
        }

        [Fact]
        public async Task Run_CreatesAnActiveAdminThatCanSignIn()
        {
            var code = await Run(new[] { Email, "--first", "New", "--last", "Admin" }, StrongPassword, StrongPassword);

            code.Should().Be(0);
            var user = await Users.FindByEmailAsync(Email);
            user.Should().NotBeNull();
            user!.IsActive.Should().BeTrue();
            user.FirstName.Should().Be("New");
            (await Users.CheckPasswordAsync(user, StrongPassword)).Should().BeTrue();
            (await Users.IsInRoleAsync(user, CrmRoles.Admin)).Should().BeTrue();
        }

        [Fact]
        public async Task Run_ForAnExistingUser_ResetsThePasswordAndReactivates()
        {
            var existing = new ApplicationUser { UserName = Email, Email = Email, IsActive = false };
            (await Users.CreateAsync(existing, "OldPassw0rd1")).Succeeded.Should().BeTrue();

            var code = await Run(new[] { Email }, StrongPassword, StrongPassword);

            code.Should().Be(0);
            var user = await Users.FindByEmailAsync(Email);
            user!.IsActive.Should().BeTrue();
            (await Users.CheckPasswordAsync(user, StrongPassword)).Should().BeTrue();
            (await Users.CheckPasswordAsync(user, "OldPassw0rd1")).Should().BeFalse();
        }

        [Fact]
        public async Task Run_WithMismatchedConfirmation_CreatesNothing()
        {
            var code = await Run(new[] { Email }, StrongPassword, "Different1234");

            code.Should().NotBe(0);
            (await Users.FindByEmailAsync(Email)).Should().BeNull();
        }

        [Fact]
        public async Task Run_WithWeakPasswordForExistingUser_KeepsTheOldPassword()
        {
            var existing = new ApplicationUser { UserName = Email, Email = Email };
            (await Users.CreateAsync(existing, "OldPassw0rd1")).Succeeded.Should().BeTrue();

            var code = await Run(new[] { Email }, "short", "short");

            code.Should().NotBe(0);
            (await Users.CheckPasswordAsync((await Users.FindByEmailAsync(Email))!, "OldPassw0rd1")).Should().BeTrue();
        }

        [Fact]
        public async Task Run_RejectsMissingEmailAndUnknownRole()
        {
            (await Run(Array.Empty<string>(), StrongPassword, StrongPassword)).Should().NotBe(0);
            (await Run(new[] { Email, "--role", "Owner" }, StrongPassword, StrongPassword)).Should().NotBe(0);
            (await Users.FindByEmailAsync(Email)).Should().BeNull();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _root.Dispose();
        }
    }
}
