using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CRM
{
    public class TaskServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly TaskService _service;

        public TaskServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new TaskService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesTask()
        {
            // Arrange
            var deal = new Deal { Name = "Proposal Deal", Value = 1000m };
            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();

            var input = new TaskInput
            {
                Title = "Follow up with client regarding proposal",
                Description = "Check if they reviewed the terms.",
                Priority = TaskPriority.High,
                Status = CrmTaskStatus.Open,
                DueDate = DateTime.UtcNow.AddDays(3),
                EntityType = CrmEntityType.Deal,
                EntityId = deal.Id
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Title.Should().Be("Follow up with client regarding proposal");
            result.Value.Priority.Should().Be(TaskPriority.High);

            var inDb = await _db.CrmTasks.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.Title.Should().Be("Follow up with client regarding proposal");
        }

        [Fact]
        public async Task SetStatusAsync_TogglesTaskStatus()
        {
            // Arrange
            var task = new CrmTask
            {
                Title = "Review Security Compliance",
                Status = CrmTaskStatus.Open
            };
            _db.CrmTasks.Add(task);
            await _db.SaveChangesAsync();

            // Act - Complete Task
            var result = await _service.SetStatusAsync(task.Id, CrmTaskStatus.Done);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.Status.Should().Be(CrmTaskStatus.Done);

            var inDb = await _db.CrmTasks.FindAsync(task.Id);
            inDb!.Status.Should().Be(CrmTaskStatus.Done);
        }

        [Fact]
        public async Task SearchAsync_FiltersByOpenOnlyAndKeyword()
        {
            // Arrange
            _db.CrmTasks.AddRange(
                new CrmTask { Title = "Urgent Bug Fix", Status = CrmTaskStatus.Open, Priority = TaskPriority.High },
                new CrmTask { Title = "Quarterly Report", Status = CrmTaskStatus.Done, Priority = TaskPriority.Low },
                new CrmTask { Title = "Client Contract", Status = CrmTaskStatus.Open, Priority = TaskPriority.Medium }
            );
            await _db.SaveChangesAsync();

            // Act
            var openOnly = await _service.SearchAsync(new TaskQuery { OpenOnly = true });
            var bugQuery = await _service.SearchAsync(new TaskQuery { Q = "Bug" });

            // Assert
            openOnly.TotalCount.Should().Be(2);
            bugQuery.TotalCount.Should().Be(1);
            bugQuery.Items[0].Title.Should().Be("Urgent Bug Fix");
        }

        [Fact]
        public async Task DeleteAsync_RemovesTask()
        {
            // Arrange
            var task = new CrmTask { Title = "Task to delete" };
            _db.CrmTasks.Add(task);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(task.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            (await _db.CrmTasks.FindAsync(task.Id)).Should().BeNull();
        }
    }
}
