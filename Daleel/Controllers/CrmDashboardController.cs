using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    /// <summary>
    /// Landing page of the private CRM area: headline KPIs, open pipeline value,
    /// upcoming tasks and the most recent activity.
    /// </summary>
    [Route("crm")]
    [Authorize]
    public class CrmDashboardController : Controller
    {
        private readonly IDashboardService _dashboard;

        public CrmDashboardController(IDashboardService dashboard) => _dashboard = dashboard;

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                Kpis = await _dashboard.GetKpisAsync(),
                Pipeline = await _dashboard.GetPipelineByCurrencyAsync(),
                UpcomingTasks = await _dashboard.GetUpcomingTasksAsync(5),
                RecentActivities = await _dashboard.GetRecentActivitiesAsync(6)
            };

            return View(model);
        }
    }
}
