using Daleel.BAL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    [Route("cms")]
    [Authorize(Roles = "Admin")]
    public class CmsDashboardController : Controller
    {
        private readonly IArticleService _articles;

        public CmsDashboardController(IArticleService articles)
        {
            _articles = articles;
        }

        [HttpGet("")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Index()
        {
            var stats = await _articles.GetDashboardStatsAsync();
            return View(stats);
        }
    }
}
