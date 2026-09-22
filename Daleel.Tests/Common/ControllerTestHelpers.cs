using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Daleel.Tests.Common
{
    public static class ControllerTestHelpers
    {
        public static T WithTestContext<T>(this T controller, ClaimsPrincipal? user = null, string? queryBrand = null) where T : Controller
        {
            var httpContext = new DefaultHttpContext();
            if (user != null)
            {
                httpContext.User = user;
            }

            if (!string.IsNullOrWhiteSpace(queryBrand))
            {
                httpContext.Request.QueryString = new QueryString($"?brand={queryBrand}");
            }

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = tempData;

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>()))
                .Returns<string>(url => !string.IsNullOrEmpty(url) && url.StartsWith("/") && !url.StartsWith("//"));
            mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("/mock-action");

            controller.Url = mockUrlHelper.Object;

            return controller;
        }
    }
}
