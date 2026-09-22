using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CRM
{
    public class ActivityServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly ActivityService _service;

        public ActivityServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new ActivityService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_LogsActivity()
        {
            // Arrange
            var lead = new Lead { FirstName = "Target", LastName = "Lead" };
            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            var input = new ActivityInput
            {
                EntityType = CrmEntityType.Lead,
                EntityId = lead.Id,
                Type = ActivityType.Call,
                Subject = "Initial Discovery Phone Call",
                Notes = "Client interested in annual enterprise license.",
                OccurredAt = DateTime.UtcNow
            };

            // Act
            var result = await _service.CreateAsync(input, createdByUserId: null);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Subject.Should().Be("Initial Discovery Phone Call");
            result.Value.Type.Should().Be(ActivityType.Call);

            var inDb = await _db.Activities.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.EntityType.Should().Be(CrmEntityType.Lead);
            inDb.EntityId.Should().Be(lead.Id);
        }

        [Fact]
        public async Task GetForEntityAsync_ReturnsOrderedActivitiesForTargetEntity()
        {
            // Arrange
            _db.Activities.AddRange(
                new Activity
                {
                    EntityType = CrmEntityType.Company,
                    EntityId = 1,
                    Subject = "First Call",
                    Type = ActivityType.Call,
                    OccurredAt = DateTime.UtcNow.AddDays(-2)
                },
                new Activity
                {
                    EntityType = CrmEntityType.Company,
                    EntityId = 1,
                    Subject = "Recent Meeting",
                    Type = ActivityType.Meeting,
                    OccurredAt = DateTime.UtcNow.AddDays(-1)
                },
                new Activity
                {
                    EntityType = CrmEntityType.Company,
                    EntityId = 2,
                    Subject = "Other Company Activity",
                    Type = ActivityType.Note,
                    OccurredAt = DateTime.UtcNow
                }
            );
            await _db.SaveChangesAsync();

            // Act
            var list = await _service.GetForEntityAsync(CrmEntityType.Company, 1);

            // Assert
            list.Should().HaveCount(2);
            list[0].Subject.Should().Be("Recent Meeting");
            list[1].Subject.Should().Be("First Call");
        }

        [Fact]
        public async Task DeleteAsync_RemovesActivity()
        {
            // Arrange
            var act = new Activity { EntityType = CrmEntityType.Deal, EntityId = 10, Subject = "To Delete" };
            _db.Activities.Add(act);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(act.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            (await _db.Activities.FindAsync(act.Id)).Should().BeNull();
        }
    }
}
