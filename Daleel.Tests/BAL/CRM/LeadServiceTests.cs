using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;
using Microsoft.Extensions.Logging;

namespace Daleel.Tests.BAL.CRM
{
    public class LeadServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<ILogger<LeadService>> _mockLogger;
        private readonly LeadService _service;

        public LeadServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _mockLogger = new Mock<ILogger<LeadService>>();
            _service = new LeadService(_db, _mockLogger.Object);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesLead()
        {
            // Arrange
            var input = new LeadInput
            {
                FirstName = "Majid",
                LastName = "Al-Subaie",
                Email = "majid@subaie.example.com",
                Phone = "+966501112233",
                CompanyName = "Subaie Enterprises",
                JobTitle = "Operations Manager",
                Source = LeadSource.Event,
                Status = LeadStatus.New
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.FullName.Should().Be("Majid Al-Subaie");
            result.Value.Status.Should().Be(LeadStatus.New);

            var inDb = await _db.Leads.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.Email.Should().Be("majid@subaie.example.com");
        }

        [Fact]
        public async Task SearchAsync_FiltersByStatusAndArchiveFlag()
        {
            // Arrange
            _db.Leads.AddRange(
                new Lead { FirstName = "Active", LastName = "Lead 1", Status = LeadStatus.New, IsArchived = false },
                new Lead { FirstName = "Qualified", LastName = "Lead 2", Status = LeadStatus.Qualified, IsArchived = false },
                new Lead { FirstName = "Archived", LastName = "Lead 3", Status = LeadStatus.New, IsArchived = true }
            );
            await _db.SaveChangesAsync();

            // Act
            var activeNew = await _service.SearchAsync(new LeadQuery { Status = LeadStatus.New, ShowArchived = false });
            var includingArchived = await _service.SearchAsync(new LeadQuery { Status = LeadStatus.New, ShowArchived = true });

            // Assert
            activeNew.TotalCount.Should().Be(1);
            activeNew.Items[0].FirstName.Should().Be("Active");

            includingArchived.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task CaptureAsync_PublicFormSubmission_CreatesNewLead()
        {
            // Arrange
            var input = new LeadCaptureInput
            {
                FirstName = "Visitor",
                LastName = "Prospect",
                Email = "visitor@prospect.com",
                CompanyName = "Prospect Co",
                Source = LeadSource.Website
            };

            // Act
            var success = await _service.CaptureAsync(input);

            // Assert
            success.Should().BeTrue();
            var inDb = _db.Leads.FirstOrDefault(l => l.Email == "visitor@prospect.com");
            inDb.Should().NotBeNull();
            inDb!.Status.Should().Be(LeadStatus.New);
            inDb.Source.Should().Be(LeadSource.Website);
        }

        [Fact]
        public async Task SetArchivedAsync_ArchivesAndUnarchivesLead()
        {
            // Arrange
            var lead = new Lead { FirstName = "Lead", LastName = "Test", IsArchived = false };
            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            // Act - Archive
            var archiveResult = await _service.SetArchivedAsync(lead.Id, true);

            // Assert archive
            archiveResult.Succeeded.Should().BeTrue();
            archiveResult.Value!.IsArchived.Should().BeTrue();

            // Act - Unarchive
            var unarchiveResult = await _service.SetArchivedAsync(lead.Id, false);

            // Assert unarchive
            unarchiveResult.Succeeded.Should().BeTrue();
            unarchiveResult.Value!.IsArchived.Should().BeFalse();
        }

        [Fact]
        public async Task ConvertAsync_CreatesNewCompanyAndContactAndMarksLeadConverted()
        {
            // Arrange
            var lead = new Lead
            {
                FirstName = "Hassan",
                LastName = "Al-Amri",
                Email = "hassan@alamri.sa",
                Phone = "+966540001122",
                CompanyName = "Al-Amri Trading",
                JobTitle = "Chief Executive Officer",
                Status = LeadStatus.Qualified
            };
            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            var conversionInput = new LeadConversionInput
            {
                FirstName = "Hassan",
                LastName = "Al-Amri",
                Email = "hassan@alamri.sa",
                Phone = "+966540001122",
                JobTitle = "CEO",
                CreateNewCompany = true,
                NewCompanyName = "Al-Amri Trading Group"
            };

            // Act
            var result = await _service.ConvertAsync(lead.Id, conversionInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.ContactId.Should().BeGreaterThan(0);
            result.Value.ContactName.Should().Be("Hassan Al-Amri");

            // Verify lead updated status
            var updatedLead = await _db.Leads.FindAsync(lead.Id);
            updatedLead!.Status.Should().Be(LeadStatus.Converted);
            updatedLead.ConvertedAt.Should().NotBeNull();
            updatedLead.ConvertedContactId.Should().Be(result.Value.ContactId);

            // Verify contact created and linked
            var contact = await _db.Contacts.FindAsync(result.Value.ContactId);
            contact.Should().NotBeNull();
            contact!.FullName.Should().Be("Hassan Al-Amri");
            contact.CompanyId.Should().NotBeNull();
        }

        [Fact]
        public async Task ConvertAsync_DuplicateNormalizedCompanyName_IsRejected()
        {
            var lead = new Lead
            {
                FirstName = "New",
                LastName = "Lead",
                Status = LeadStatus.Qualified
            };
            _db.Leads.Add(lead);
            _db.Companies.Add(new Company { Name = "Existing Company" });
            await _db.SaveChangesAsync();

            var result = await _service.ConvertAsync(lead.Id, new LeadConversionInput
            {
                FirstName = "New",
                LastName = "Lead",
                CreateNewCompany = true,
                NewCompanyName = "  EXISTING COMPANY  "
            });

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle(e =>
                e.Field == nameof(LeadConversionInput.NewCompanyName) &&
                e.Message.Contains("already exists"));
            _db.Companies.Should().HaveCount(1);
            _db.Contacts.Should().BeEmpty();
        }

        [Fact]
        public async Task ConvertAsync_AlreadyConvertedLead_FailsConversion()
        {
            // Arrange
            var lead = new Lead
            {
                FirstName = "Already",
                LastName = "Converted",
                Status = LeadStatus.Converted,
                ConvertedAt = DateTime.UtcNow
            };
            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            var conversionInput = new LeadConversionInput
            {
                FirstName = "Already",
                LastName = "Converted",
                CreateNewCompany = false
            };

            // Act
            var result = await _service.ConvertAsync(lead.Id, conversionInput);

            // Assert
            result.Succeeded.Should().BeFalse();
        }
    }
}
