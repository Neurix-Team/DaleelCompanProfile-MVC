using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CRM
{
    public class DashboardServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly DashboardService _service;

        public DashboardServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new DashboardService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task GetKpisAsync_CalculatesAllCrmMetricsAccurately()
        {
            // Arrange
            _db.Leads.AddRange(
                new Lead { FirstName = "L1", LastName = "New", Status = LeadStatus.New, IsArchived = false },
                new Lead { FirstName = "L2", LastName = "Qual", Status = LeadStatus.Qualified, IsArchived = false },
                new Lead { FirstName = "L3", LastName = "Conv", Status = LeadStatus.Converted, IsArchived = false },
                new Lead { FirstName = "L4", LastName = "Arch", Status = LeadStatus.New, IsArchived = true }
            );

            _db.Companies.AddRange(
                new Company { Name = "C1", Status = RecordStatus.Active },
                new Company { Name = "C2", Status = RecordStatus.Inactive }
            );

            _db.Contacts.AddRange(
                new Contact { FirstName = "Ct1", LastName = "A", Status = RecordStatus.Active },
                new Contact { FirstName = "Ct2", LastName = "B", Status = RecordStatus.Inactive }
            );

            _db.Deals.AddRange(
                new Deal { Name = "D1", Value = 1000m, Stage = DealStage.New },
                new Deal { Name = "D2", Value = 2000m, Stage = DealStage.Won },
                new Deal { Name = "D3", Value = 3000m, Stage = DealStage.Lost }
            );

            _db.CrmTasks.AddRange(
                new CrmTask { Title = "T1", Status = CrmTaskStatus.Open, DueDate = DateTime.UtcNow.AddDays(1) },
                new CrmTask { Title = "T2", Status = CrmTaskStatus.Open, DueDate = DateTime.UtcNow.AddDays(-2) }, // Overdue
                new CrmTask { Title = "T3", Status = CrmTaskStatus.Done }
            );

            await _db.SaveChangesAsync();

            // Act
            var kpis = await _service.GetKpisAsync();

            // Assert
            kpis.TotalLeads.Should().Be(3); // Excludes archived
            kpis.NewLeads.Should().Be(1);
            kpis.QualifiedLeads.Should().Be(1);
            kpis.ConvertedLeads.Should().Be(1);

            kpis.TotalCompanies.Should().Be(1); // Active only
            kpis.TotalCustomers.Should().Be(1); // Active only

            kpis.OpenDeals.Should().Be(1);
            kpis.WonDeals.Should().Be(1);
            kpis.LostDeals.Should().Be(1);

            kpis.OpenTasks.Should().Be(2);
            kpis.OverdueTasks.Should().Be(1);
        }

        [Fact]
        public async Task GetPipelineByCurrencyAsync_AggregatesOpenDealsPerCurrency()
        {
            // Arrange
            _db.Deals.AddRange(
                new Deal { Name = "SAR Deal 1", Value = 10000m, Currency = "SAR", Stage = DealStage.New },
                new Deal { Name = "SAR Deal 2", Value = 20000m, Currency = "SAR", Stage = DealStage.Proposal },
                new Deal { Name = "SAR Won", Value = 50000m, Currency = "SAR", Stage = DealStage.Won }, // Won excluded from open
                new Deal { Name = "USD Deal", Value = 5000m, Currency = "USD", Stage = DealStage.New }
            );
            await _db.SaveChangesAsync();

            // Act
            var totals = await _service.GetPipelineByCurrencyAsync();

            // Assert
            totals.Should().HaveCount(2);

            var sar = totals.First(t => t.Currency == "SAR");
            sar.Total.Should().Be(30000m);
            sar.Count.Should().Be(2);

            var usd = totals.First(t => t.Currency == "USD");
            usd.Total.Should().Be(5000m);
            usd.Count.Should().Be(1);
        }
    }
}
