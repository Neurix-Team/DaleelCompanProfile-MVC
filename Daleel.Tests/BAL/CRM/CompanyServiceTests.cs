using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CRM
{
    public class CompanyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly CompanyService _service;

        public CompanyServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new CompanyService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesCompany()
        {
            // Arrange
            var input = new CompanyInput
            {
                Name = "Saudi Telecom Solutions",
                Industry = "Telecommunications",
                Website = "https://stc-solutions.example.com",
                Phone = "+966112223344",
                Email = "info@stc-solutions.example.com",
                City = "Riyadh",
                Country = "Saudi Arabia",
                Status = RecordStatus.Active
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Name.Should().Be("Saudi Telecom Solutions");
            result.Value.Id.Should().BeGreaterThan(0);

            var inDb = await _db.Companies.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.Name.Should().Be("Saudi Telecom Solutions");
        }

        [Fact]
        public async Task CreateAsync_DuplicateNormalizedName_IsRejected()
        {
            _db.Companies.Add(new Company
            {
                Name = "Acme Holdings",
                Email = "first@acme.example"
            });
            await _db.SaveChangesAsync();

            var result = await _service.CreateAsync(new CompanyInput
            {
                Name = "  ACME HOLDINGS  ",
                Email = "second@acme.example"
            });

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle(e =>
                e.Field == nameof(CompanyInput.Name) && e.Message.Contains("already exists"));
            _db.Companies.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateAsync_DuplicateNormalizedEmail_IsRejected()
        {
            _db.Companies.Add(new Company
            {
                Name = "First Company",
                Email = "contact@example.com"
            });
            await _db.SaveChangesAsync();

            var result = await _service.CreateAsync(new CompanyInput
            {
                Name = "Second Company",
                Email = "  CONTACT@EXAMPLE.COM  "
            });

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle(e =>
                e.Field == nameof(CompanyInput.Email) && e.Message.Contains("already exists"));
            _db.Companies.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchAsync_FiltersByKeywordAndStatus()
        {
            // Arrange
            _db.Companies.AddRange(
                new Company { Name = "Aramco Digital", Industry = "Energy & Tech", City = "Dhahran", Status = RecordStatus.Active },
                new Company { Name = "SABIC Tech", Industry = "Petrochemicals", City = "Jubail", Status = RecordStatus.Active },
                new Company { Name = "Old Inactive Corp", Industry = "Retail", City = "Riyadh", Status = RecordStatus.Inactive }
            );
            await _db.SaveChangesAsync();

            // Act
            var activeResults = await _service.SearchAsync(new CompanyQuery { Status = RecordStatus.Active });
            var queryResults = await _service.SearchAsync(new CompanyQuery { Q = "Aramco" });

            // Assert
            activeResults.TotalCount.Should().Be(2);
            queryResults.TotalCount.Should().Be(1);
            queryResults.Items[0].Name.Should().Be("Aramco Digital");
        }

        [Fact]
        public async Task GetWithContactsAsync_ReturnsCompanyWithAssociatedContacts()
        {
            // Arrange
            var company = new Company { Name = "Tech Giant", Status = RecordStatus.Active };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            _db.Contacts.AddRange(
                new Contact { CompanyId = company.Id, FirstName = "Ahmed", LastName = "Ali", Email = "ahmed@tech.com" },
                new Contact { CompanyId = company.Id, FirstName = "Sara", LastName = "Hassan", Email = "sara@tech.com" }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetWithContactsAsync(company.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Contacts.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_ModifiesFields()
        {
            // Arrange
            var company = new Company { Name = "Initial Company", City = "Jeddah", Status = RecordStatus.Active };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var updateInput = new CompanyInput
            {
                Name = "Updated Company Name",
                City = "Riyadh",
                Status = RecordStatus.Inactive
            };

            // Act
            var result = await _service.UpdateAsync(company.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.Name.Should().Be("Updated Company Name");
            result.Value.City.Should().Be("Riyadh");
            result.Value.Status.Should().Be(RecordStatus.Inactive);
        }

        [Fact]
        public async Task UpdateAsync_UnchangedNameAndEmail_DoesNotConflictWithItself()
        {
            var company = new Company
            {
                Name = "Existing Company",
                Email = "hello@example.com",
                Status = RecordStatus.Active
            };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var result = await _service.UpdateAsync(company.Id, new CompanyInput
            {
                Name = "Existing Company",
                Email = "hello@example.com",
                City = "Riyadh",
                Status = RecordStatus.Active
            });

            result.Succeeded.Should().BeTrue();
            result.Value!.City.Should().Be("Riyadh");
        }

        [Fact]
        public async Task UpdateAsync_DuplicateNameAndEmail_ReturnsBothFieldErrors()
        {
            var first = new Company { Name = "First Company", Email = "first@example.com" };
            var second = new Company { Name = "Second Company", Email = "second@example.com" };
            _db.Companies.AddRange(first, second);
            await _db.SaveChangesAsync();

            var result = await _service.UpdateAsync(second.Id, new CompanyInput
            {
                Name = " first company ",
                Email = "FIRST@EXAMPLE.COM"
            });

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == nameof(CompanyInput.Name));
            result.Errors.Should().Contain(e => e.Field == nameof(CompanyInput.Email));
            second.Name.Should().Be("Second Company");
            second.Email.Should().Be("second@example.com");
        }

        [Fact]
        public async Task DeleteAsync_RemovesCompanyAndCountsOrphanedContacts()
        {
            // Arrange
            var company = new Company { Name = "To Delete Co", Status = RecordStatus.Active };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            _db.Contacts.Add(new Contact { CompanyId = company.Id, FirstName = "Omar", LastName = "Zaid" });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(company.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Name.Should().Be("To Delete Co");
            result.Value.OrphanedContacts.Should().Be(1);

            (await _db.Companies.FindAsync(company.Id)).Should().BeNull();
        }

        [Fact]
        public async Task GetOptionsAsync_ReturnsOrderedDropdownChoices()
        {
            // Arrange
            _db.Companies.AddRange(
                new Company { Name = "Zeta Logistics" },
                new Company { Name = "Alpha Tech" }
            );
            await _db.SaveChangesAsync();

            // Act
            var options = await _service.GetOptionsAsync();

            // Assert
            options.Should().HaveCount(2);
            options[0].Text.Should().Be("Alpha Tech");
            options[1].Text.Should().Be("Zeta Logistics");
        }
    }
}
