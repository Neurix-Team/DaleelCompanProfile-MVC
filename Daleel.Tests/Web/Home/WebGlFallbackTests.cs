namespace Daleel.Tests.Web.Home
{
    public class WebGlFallbackTests
    {
        [Fact]
        public void HeroAssets_KeepAStaticFallbackForWebGlFailures()
        {
            var root = FindSolutionRoot();
            var script = File.ReadAllText(Path.Combine(root, "Daleel", "wwwroot", "js", "hero-scrolly.js"));
            var styles = File.ReadAllText(Path.Combine(root, "Daleel", "wwwroot", "css", "site.css"));

            script.Should().Contain("function supportsWebGL()");
            script.Should().Contain("function activateWebGLFallback(reason)");
            script.Should().Contain("webglcontextlost");
            script.Should().Contain("createWebGLRenderer(canvas)");
            styles.Should().Contain(".webgl-fallback #scrolly-canvas");
            styles.Should().Contain(".webgl-fallback-surface::before");
        }

        private static string FindSolutionRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Daleel.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the Daleel solution root.");
        }
    }
}
