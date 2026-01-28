using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace CustomLogo
{
    [ApiController]
    public class WebController : ControllerBase
    {
        private readonly IApplicationPaths _appPaths;
        private readonly string _logoDirectory;

        public WebController(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
            _logoDirectory = Path.Combine(appPaths.PluginConfigurationsPath, "CustomLogo");

            if (!Directory.Exists(_logoDirectory))
            {
                Directory.CreateDirectory(_logoDirectory);
            }
        }

        [HttpPost("logo/upload")]
        public async Task<IActionResult> UploadLogo()
        {
            var logo = Request.Form.Files["Logo"];
            var bannerDark = Request.Form.Files["BannerDark"];
            var bannerLight = Request.Form.Files["BannerLight"];

            try
            {
                if (logo != null && logo.Length > 0)
                {
                    var path = Path.Combine(_logoDirectory, "icon-transparent.png");
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await logo.CopyToAsync(stream).ConfigureAwait(false);
                    }
                }

                if (bannerDark != null && bannerDark.Length > 0)
                {
                    var path = Path.Combine(_logoDirectory, "banner-dark.png");
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await bannerDark.CopyToAsync(stream).ConfigureAwait(false);
                    }
                }

                if (bannerLight != null && bannerLight.Length > 0)
                {
                    var path = Path.Combine(_logoDirectory, "banner-light.png");
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await bannerLight.CopyToAsync(stream).ConfigureAwait(false);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Content(
                    "<html><head></head><body>Jellyfin does not have write access to the plugin data directory. Please check the permissions.<br><a href='/web/#/dashboard/plugins'>Return to Jellyfin</a></body></html>",
                    "text/html");
            }
            catch (Exception ex)
            {
                return Content(
                    $"<html><head></head><body>An error occurred: {ex.Message}<br><a href='/web/#/dashboard/plugins'>Return to Jellyfin</a></body></html>",
                    "text/html");
            }

            return Content(
                "<html><head><meta http-equiv='refresh' content='0;url=/web/#/dashboard/plugins' /></head><body>Redirection...</body></html>",
                "text/html");
        }

        [HttpGet("logo/icon")]
        public IActionResult GetIcon()
        {
            var path = Path.Combine(_logoDirectory, "icon-transparent.png");
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        [HttpGet("logo/banner-dark")]
        public IActionResult GetBannerDark()
        {
            var path = Path.Combine(_logoDirectory, "banner-dark.png");
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        [HttpGet("logo/banner-light")]
        public IActionResult GetBannerLight()
        {
            var path = Path.Combine(_logoDirectory, "banner-light.png");
            if (System.IO.File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                return File(bytes, "image/png");
            }

            return NotFound();
        }

        [HttpDelete("logo/icon")]
        public IActionResult DeleteIcon()
        {
            var path = Path.Combine(_logoDirectory, "icon-transparent.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok();
            }

            return NotFound();
        }

        [HttpDelete("logo/banner-dark")]
        public IActionResult DeleteBannerDark()
        {
            var path = Path.Combine(_logoDirectory, "banner-dark.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok();
            }

            return NotFound();
        }

        [HttpDelete("logo/banner-light")]
        public IActionResult DeleteBannerLight()
        {
            var path = Path.Combine(_logoDirectory, "banner-light.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok();
            }

            return NotFound();
        }
    }
}