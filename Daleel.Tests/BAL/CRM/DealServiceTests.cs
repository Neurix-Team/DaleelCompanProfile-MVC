using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CRM
{
    public class DealServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly DealService _service;

        public DealServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new DealService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesDeal()
        {
            // Arrange
            var company = new Company { Name = "Riyadh Logistics" };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            var input = new DealInput
            {
                Name = "Enterprise ERP & AI Integration Deal",
                CompanyId = company.Id,
                Value = 150000m,
                Currency = "SAR",
                Stage = DealStage.New,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(2),
                Description = "Full stack rollout across all branches."
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Name.Should().Be("Enterprise ERP & AI Integration Deal");
            result.Value.Value.Should().Be(150000m);
            result.Value.Stage.Should().Be(DealStage.New);

            var inDb = await _db.Deals.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.Value.Should().Be(150000m);
        }

        [Fact]
        public async Task MoveToStageAsync_UpdatesDealStage()
        {
            // Arrange
            var deal = new Deal
            {
                Name = "Stage Progression Deal",
                Value = 50000m,
                Currency = "USD",
                Stage = DealStage.Proposal
            };
            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();

            // Act - Move to Negotiation
            var result = await _service.MoveToStageAsync(deal.Id, DealStage.Negotiation);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.Stage.Should().Be(DealStage.Negotiation);

            var inDb = await _db.Deals.FindAsync(deal.Id);
            inDb!.Stage.Should().Be(DealStage.Negotiation);
        }

        [Fact]
        public async Task GetPipelineAsync_ReturnsPipelineBoardWithStageRollups()
        {
            // Arrange
            _db.Deals.AddRange(
                new Deal { Name = "Deal 1", Value = 10000m, Currency = "SAR", Stage = DealStage.New },
                new Deal { Name = "Deal 2", Value = 25000m, Currency = "SAR", Stage = DealStage.Proposal },
                new Deal { Name = "Deal 3", Value = 50000m, Currency = "SAR", Stage = DealStage.Won }
            );
            await _db.SaveChangesAsync();

            // Act
            var board = await _service.GetPipelineAsync();

            // Assert
            board.Should().NotBeNull();
            board.TotalCount.Should().Be(3);
            board.OpenValue.Should().Be(35000m); // Excludes Won (50000)
            board.Currency.Should().Be("SAR");
            board.IsMixedCurrency.Should().BeFalse();

            var newCol = board.Columns.First(c => c.Stage == DealStage.New);
            newCol.Count.Should().Be(1);
            newCol.TotalValue.Should().Be(10000m);
        }

        [Fact]
        public async Task DeleteAsync_RemovesDeal()
        {
            // Arrange
            var deal = new Deal { Name = "Deal to Delete", Value = 1000m, Currency = "USD" };
            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(deal.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            (await _db.Deals.FindAsync(deal.Id)).Should().BeNull();
        }

        [Fact]
        public void GetCurrencies_ReturnsSupportedCurrencies()
        {
            // Act
            var currencies = _service.GetCurrencies();

            // Assert
            currencies.Should().Contain("SAR");
            currencies.Should().Contain("USD");
            currencies.Should().Contain("EUR");
        }
    }
}
