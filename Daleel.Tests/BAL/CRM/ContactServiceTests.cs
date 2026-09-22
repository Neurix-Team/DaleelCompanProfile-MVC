using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CRM
{
    public class ContactServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly ContactService _service;

        public ContactServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new ContactService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesContact()
        {
            // Arrange
            var company = new Company { Name = "Al-Rajhi Group" };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var input = new ContactInput
            {
                FirstName = "Yousef",
                LastName = "Al-Mansoori",
                Email = "yousef@alrajhi.com",
                Phone = "+966551234567",
                JobTitle = "VP of Procurement",
                CompanyId = company.Id,
                Status = RecordStatus.Active,
                Notes = "Key decision maker for enterprise SaaS deals."
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.FirstName.Should().Be("Yousef");
            result.Value.FullName.Should().Be("Yousef Al-Mansoori");
            result.Value.CompanyId.Should().Be(company.Id);

            var inDb = await _db.Contacts.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.Email.Should().Be("yousef@alrajhi.com");
        }

        [Fact]
        public async Task CreateAsync_InvalidCompanyId_ReturnsValidationError()
        {
            // Arrange
            var input = new ContactInput
            {
                FirstName = "Test",
                LastName = "User",
                CompanyId = 99999
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == nameof(ContactInput.CompanyId));
        }

        [Fact]
        public async Task SearchAsync_FiltersByKeywordAndStatus()
        {
            // Arrange
            var company = new Company { Name = "Saudi Aramco" };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            _db.Contacts.AddRange(
                new Contact { FirstName = "Nasser", LastName = "Al-Dosari", CompanyId = company.Id, Status = RecordStatus.Active },
                new Contact { FirstName = "Fatima", LastName = "Al-Harbi", Status = RecordStatus.Active },
                new Contact { FirstName = "Inactive", LastName = "Person", Status = RecordStatus.Inactive }
            );
            await _db.SaveChangesAsync();

            // Act
            var active = await _service.SearchAsync(new ContactQuery { Status = RecordStatus.Active });
            var query = await _service.SearchAsync(new ContactQuery { Q = "Aramco" });

            // Assert
            active.TotalCount.Should().Be(2);
            query.TotalCount.Should().Be(1);
            query.Items[0].FullName.Should().Be("Nasser Al-Dosari");
        }

        [Fact]
        public async Task GetWithCompanyAsync_ReturnsContactAndCompanyDetails()
        {
            // Arrange
            var company = new Company { Name = "Acme Corp" };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var contact = new Contact { CompanyId = company.Id, FirstName = "John", LastName = "Doe" };
            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetWithCompanyAsync(contact.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Company.Should().NotBeNull();
            result.Company!.Name.Should().Be("Acme Corp");
        }

        [Fact]
        public async Task UpdateAsync_ModifiesFields()
        {
            // Arrange
            var contact = new Contact { FirstName = "Original", LastName = "Name", Status = RecordStatus.Active };
            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();

            var updateInput = new ContactInput
            {
                FirstName = "UpdatedFirst",
                LastName = "UpdatedLast",
                JobTitle = "Senior Director",
                Status = RecordStatus.Inactive
            };

            // Act
            var result = await _service.UpdateAsync(contact.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.FirstName.Should().Be("UpdatedFirst");
            result.Value.JobTitle.Should().Be("Senior Director");
            result.Value.Status.Should().Be(RecordStatus.Inactive);
        }

        [Fact]
        public async Task DeleteAsync_RemovesContact()
        {
            // Arrange
            var contact = new Contact { FirstName = "To", LastName = "Delete" };
            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(contact.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().Be("To Delete");
            (await _db.Contacts.FindAsync(contact.Id)).Should().BeNull();
        }
    }
}
