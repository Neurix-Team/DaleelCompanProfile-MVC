using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl = null)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" }
            );

            returnUrl ??= GetRefererLocalPath();

            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            return LocalRedirect(returnUrl);
        }

        [HttpGet("/ar")]
        public IActionResult SetArabic(string? returnUrl = null)
        {
            return SetLanguage("ar", returnUrl);
        }

        [HttpGet("/en")]
        public IActionResult SetEnglish(string? returnUrl = null)
        {
            return SetLanguage("en", returnUrl);
        }

        private string? GetRefererLocalPath()
        {
            var referer = Request.Headers.Referer.ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            {
                var local = uri.PathAndQuery;
                if (Url.IsLocalUrl(local))
                {
                    return local;
                }
            }
            return null;
        }
    }
}
